using Application.Common.CQS.Queries;
using Application.Common.Repositories;
using Application.Common.Services;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.OrderManager.Commands;

public class CreateOrderResult
{
    public Order? Data { get; set; }
}

public class CreateOrderRequest : IRequest<CreateOrderResult>
{
    public string? CustomerId { get; init; }
    public string? Status { get; init; }
    public decimal? Discount { get; init; }
    public decimal? Taxes { get; init; }
    public decimal? TotalAmount { get; init; }
    public decimal? ShippingCost { get; init; }
    public string? Notes { get; init; }
    public string? ShippingBoxId { get; init; }
    public PaymentEditDto? Payment { get; init; }
    public ShippingDetailEditDto? ShippingDetail { get; init; }
    public List<OrderDetailEditDto>? OrderDetails { get; init; }
}

public class CreateOrderValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.OrderDetails).NotEmpty();
    }
}

public class CreateOrderHandler : IRequestHandler<CreateOrderRequest, CreateOrderResult>
{
    private readonly ICommandRepository<Order> _repository;
    private readonly ICommandRepository<Product> _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IQueryContext _context;
    private readonly IShippingCostService _shippingCostService;

    public CreateOrderHandler(
        ICommandRepository<Order> repository,
        ICommandRepository<Product> productRepository,
        IUnitOfWork unitOfWork,
        IQueryContext context,
        IShippingCostService shippingCostService)
    {
        _repository = repository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _context = context;
        _shippingCostService = shippingCostService;
    }

    public async Task<CreateOrderResult> Handle(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var customer = await _context.Customer
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == request.CustomerId, cancellationToken);

        if (customer == null)
        {
            throw new Exception($"Customer not found: {request.CustomerId}");
        }

        EnsureShippingPostCodeMatchesCustomer(customer, request.ShippingDetail?.PostCode);

        ShippingBox? shippingBox = null;

        if (!string.IsNullOrWhiteSpace(request.ShippingBoxId))
        {
            shippingBox = await _context.ShippingBox
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == request.ShippingBoxId, cancellationToken);

            if (shippingBox == null)
            {
                throw new Exception($"ShippingBox not found: {request.ShippingBoxId}");
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Payment?.PaymentTypeId))
        {
            var paymentTypeExists = await _context.PaymentType
                .AnyAsync(x => x.Id == request.Payment.PaymentTypeId, cancellationToken);

            if (!paymentTypeExists)
            {
                throw new Exception($"PaymentType not found: {request.Payment.PaymentTypeId}");
            }
        }

        var entity = new Order
        {
            CustomerId = request.CustomerId,
            Discount = request.Discount,
            Taxes = request.Taxes,
            Notes = request.Notes,
            ShippingBoxId = request.ShippingBoxId,
            OrderDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        if (!string.IsNullOrWhiteSpace(request.Status) &&
            Enum.TryParse<OrderStatus>(request.Status, out var status))
        {
            entity.Status = status;
        }

        if (request.Payment != null)
        {
            entity.Payment = new Payment
            {
                Name = request.Payment.Name ?? $"Payment for order {entity.Id}",
                Description = request.Payment.Description,
                Status = !string.IsNullOrWhiteSpace(request.Payment.Status) &&
                    Enum.TryParse<PaymentStatus>(request.Payment.Status, out var paymentStatus)
                        ? paymentStatus
                        : PaymentStatus.Pending,
                PaymentDateTime = request.Payment.PaymentDateTime,
                Amount = request.Payment.Amount,
                PaymentTypeId = request.Payment.PaymentTypeId,
                PaymentDetail = new PaymentDetail
                {
                    PaymentMethod = request.Payment.PaymentMethod,
                    TransactionId = request.Payment.TransactionId,
                    AuthorizationCode = request.Payment.AuthorizationCode,
                    ReferenceNumber = request.Payment.ReferenceNumber,
                    CardHolderName = request.Payment.CardHolderName,
                    CardLast4 = request.Payment.CardLast4
                }
            };
            entity.PaymentId = entity.Payment.Id;
            entity.Payment.PaymentDetail.PaymentId = entity.Payment.Id;
        }

        if (request.ShippingDetail != null)
        {
            entity.ShippingDetail = new ShippingDetail
            {
                FirstName = request.ShippingDetail.FirstName,
                LastName = request.ShippingDetail.LastName,
                Email = request.ShippingDetail.Email,
                PhoneNumber = request.ShippingDetail.PhoneNumber,
                Street = request.ShippingDetail.Street,
                Number = request.ShippingDetail.Number,
                Neighborhood = request.ShippingDetail.Neighborhood,
                Complement = request.ShippingDetail.Complement,
                City = request.ShippingDetail.City,
                State = request.ShippingDetail.State,
                PostCode = request.ShippingDetail.PostCode,
                OrderId = entity.Id
            };
        }

        foreach (var item in request.OrderDetails ?? [])
        {
            if (string.IsNullOrWhiteSpace(item.ProductId))
            {
                continue;
            }

            var product = await _context.Product
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == item.ProductId, cancellationToken);

            if (product == null)
            {
                throw new Exception($"Product not found: {item.ProductId}");
            }

            var quantity = item.Quantity < 1 ? 1 : item.Quantity;
            var unitPrice = item.UnitPrice ?? product.UnitPrice ?? 0m;

            entity.OrderDetails.Add(new OrderDetail
            {
                OrderId = entity.Id,
                ProductId = item.ProductId,
                Quantity = quantity,
                UnitPrice = unitPrice,
                TotalPrice = unitPrice * quantity
            });
        }

        entity.TotalAmount = await CalculateTotalAsync(entity, shippingBox, cancellationToken);
        await MarkProductsUnavailableWhenPaidAsync(entity, cancellationToken);

        await _repository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new CreateOrderResult
        {
            Data = entity
        };
    }

    private async Task<decimal> CalculateTotalAsync(
        Order entity,
        ShippingBox? shippingBox,
        CancellationToken cancellationToken)
    {
        var itemsTotal = entity.OrderDetails.Sum(x => x.TotalPrice ?? ((x.UnitPrice ?? 0m) * x.Quantity));
        var shippingCost = await _shippingCostService.CalculateAsync(
            shippingBox,
            entity.ShippingDetail?.PostCode,
            cancellationToken);

        return itemsTotal
            - (entity.Discount ?? 0m)
            + (entity.Taxes ?? 0m)
            + shippingCost;
    }

    private async Task MarkProductsUnavailableWhenPaidAsync(Order entity, CancellationToken cancellationToken)
    {
        if (entity.Status != OrderStatus.Paid)
        {
            return;
        }

        var productIds = entity.OrderDetails
            .Where(x => !x.IsDeleted && !string.IsNullOrWhiteSpace(x.ProductId))
            .Select(x => x.ProductId!)
            .Distinct()
            .ToList();

        if (productIds.Count == 0)
        {
            return;
        }

        var products = await _productRepository.GetQuery()
            .Where(x => productIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        foreach (var product in products)
        {
            product.ProductAvailable = false;
            _productRepository.Update(product);
        }
    }

    private static void EnsureShippingPostCodeMatchesCustomer(Customer customer, string? shippingPostCode)
    {
        var customerPostCode = NormalizePostCode(customer.PostalCode);
        var orderPostCode = NormalizePostCode(shippingPostCode);

        if (string.IsNullOrWhiteSpace(orderPostCode))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(customerPostCode) || customerPostCode != orderPostCode)
        {
            throw new Exception("O CEP do pedido deve corresponder ao CEP do cliente selecionado.");
        }
    }

    private static string NormalizePostCode(string? postCode)
    {
        return string.IsNullOrWhiteSpace(postCode)
            ? string.Empty
            : new string(postCode.Where(char.IsDigit).ToArray());
    }
}
