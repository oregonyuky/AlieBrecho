using Application.Common;
using Application.Common.Repositories;
using Application.Common.CQS.Queries;
using Application.Common.Services;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Application.Features.OrderManager.Commands;

public record PaymentEditDto
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? Status { get; init; }
    public DateTime? PaymentDateTime { get; init; }
    public decimal? Amount { get; init; }
    public string? PaymentTypeId { get; init; }
    public string? PaymentMethod { get; init; }
    public string? TransactionId { get; init; }
    public string? AuthorizationCode { get; init; }
    public string? ReferenceNumber { get; init; }
    public string? CardHolderName { get; init; }
    public string? CardLast4 { get; init; }
}

public record ShippingDetailEditDto
{
    public string? Id { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Email { get; init; }
    public string? PhoneNumber { get; init; }
    public string? Street { get; init; }
    public string? Number { get; init; }
    public string? Neighborhood { get; init; }
    public string? Complement { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? PostCode { get; init; }
}

public record OrderDetailEditDto
{
    public string? Id { get; init; }
    public string? ProductId { get; init; }
    public int Quantity { get; init; } = 1;
    public decimal? UnitPrice { get; init; }
}

public class UpdateOrderResult
{
    public Order? Data { get; set; }
}

public class UpdateOrderRequest : IRequest<UpdateOrderResult>
{
    public string? Id { get; init; }
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

public class UpdateOrderValidator : AbstractValidator<UpdateOrderRequest>
{
    public UpdateOrderValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class UpdateOrderHandler : IRequestHandler<UpdateOrderRequest, UpdateOrderResult>
{
    private readonly ICommandRepository<Order> _repository;
    private readonly ICommandRepository<OrderDetail> _orderDetailRepository;
    private readonly ICommandRepository<Product> _productRepository;
    private readonly ICommandRepository<ShippingBox> _shippingBoxRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IQueryContext _context;
    private readonly IShippingCostService _shippingCostService;

    public UpdateOrderHandler(
        ICommandRepository<Order> repository,
        ICommandRepository<OrderDetail> orderDetailRepository,
        ICommandRepository<Product> productRepository,
        ICommandRepository<ShippingBox> shippingBoxRepository,
        IUnitOfWork unitOfWork,
        IQueryContext context,
        IShippingCostService shippingCostService)
    {
        _repository = repository;
        _orderDetailRepository = orderDetailRepository;
        _productRepository = productRepository;
        _shippingBoxRepository = shippingBoxRepository;
        _unitOfWork = unitOfWork;
        _context = context;
        _shippingCostService = shippingCostService;
    }

    public async Task<UpdateOrderResult> Handle(UpdateOrderRequest request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetQuery()
            .Include(x => x.Payment)
                .ThenInclude(x => x!.PaymentDetail)
            .Include(x => x.ShippingDetail)
            .Include(x => x.OrderDetails)
            .SingleOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Order not found: {request.Id}");
        }

        var wasPaid = entity.Status == OrderStatus.Paid;

        // Status
        if (!string.IsNullOrWhiteSpace(request.Status) &&
            Enum.TryParse<OrderStatus>(request.Status, out var status))
        {
            entity.Status = status;
            if (status == OrderStatus.Cancelled && entity.ShippingBoxStockDeducted &&
                !string.IsNullOrWhiteSpace(entity.ShippingBoxId))
            {
                var boxToRestore = await _shippingBoxRepository.GetQuery()
                    .SingleOrDefaultAsync(x => x.Id == entity.ShippingBoxId, cancellationToken);
                if (boxToRestore is not null)
                {
                    PackageStockService.RestoreOnce(entity, boxToRestore);
                    _shippingBoxRepository.Update(boxToRestore);
                }
            }
        }

        var customerId = !string.IsNullOrWhiteSpace(request.CustomerId)
            ? request.CustomerId
            : entity.CustomerId;

        var customer = await _context.Customer
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == customerId, cancellationToken);

        if (customer == null)
        {
            throw new Exception($"Customer not found: {customerId}");
        }

        var shippingPostCode = request.ShippingDetail?.PostCode ?? entity.ShippingDetail?.PostCode;
        EnsureShippingPostCodeMatchesCustomer(customer, shippingPostCode);

        // Basic fields
        if (!string.IsNullOrWhiteSpace(request.CustomerId))
        {
            entity.CustomerId = request.CustomerId;
        }

        entity.Discount = request.Discount;
        entity.Taxes = request.Taxes;
        entity.Notes = request.Notes;

        // ShippingBox validation
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

            var requiredPoints = Math.Max(entity.PackageOccupationPoints ?? 1, 1);
            var productIds = entity.OrderDetails.Where(x => !x.IsDeleted && x.ProductId != null)
                .Select(x => x.ProductId!).Distinct().ToList();
            var productWeights = await _context.Product.AsNoTracking()
                .Where(x => productIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.UnitWeight ?? 0m, cancellationToken);
            var totalWeight = entity.OrderDetails.Where(x => !x.IsDeleted && x.ProductId != null)
                .Sum(x => productWeights.GetValueOrDefault(x.ProductId!) * Math.Max(x.Quantity, 1));

            if (!shippingBox.IsActive || shippingBox.StockQuantity <= 0 ||
                shippingBox.CapacityPoints < requiredPoints ||
                (shippingBox.MaxWeight.HasValue && shippingBox.MaxWeight.Value < totalWeight))
            {
                throw new ValidationException("A embalagem selecionada nao esta ativa, sem estoque ou nao comporta o pedido.");
            }

            if (entity.ShippingBoxStockDeducted && entity.ShippingBoxId != shippingBox.Id)
            {
                var oldBox = await _shippingBoxRepository.GetQuery()
                    .SingleOrDefaultAsync(x => x.Id == entity.ShippingBoxId, cancellationToken);
                if (oldBox is not null)
                {
                    oldBox.StockQuantity++;
                    _shippingBoxRepository.Update(oldBox);
                }
                var newBox = await _shippingBoxRepository.GetQuery()
                    .SingleAsync(x => x.Id == shippingBox.Id, cancellationToken);
                newBox.StockQuantity--;
                _shippingBoxRepository.Update(newBox);
            }

            entity.PackageName = shippingBox.Name;
            entity.PackageLength = shippingBox.Length;
            entity.PackageWidth = shippingBox.Width;
            entity.PackageHeight = shippingBox.Height;
            entity.PackageWeight = shippingBox.Weight;
            entity.PackageCapacityPoints = shippingBox.CapacityPoints;
        }

        // Set ShippingBox (permite null também)
        entity.ShippingBoxId = request.ShippingBoxId;

        // Payment
        if (request.Payment != null)
        {
            var payment = entity.Payment ?? new Payment();

            payment.Name = request.Payment.Name;
            payment.Description = request.Payment.Description;

            if (!string.IsNullOrWhiteSpace(request.Payment.Status) &&
                Enum.TryParse<PaymentStatus>(request.Payment.Status, out var paymentStatus))
            {
                payment.Status = paymentStatus;
            }

            payment.PaymentDateTime = request.Payment.PaymentDateTime;
            payment.Amount = request.Payment.Amount;
            payment.PaymentTypeId = request.Payment.PaymentTypeId;

            if (payment.PaymentDetail == null)
            {
                payment.PaymentDetail = new PaymentDetail
                {
                    PaymentId = payment.Id
                };
            }

            payment.PaymentDetail.PaymentMethod = request.Payment.PaymentMethod;
            payment.PaymentDetail.TransactionId = request.Payment.TransactionId;
            payment.PaymentDetail.AuthorizationCode = request.Payment.AuthorizationCode;
            payment.PaymentDetail.ReferenceNumber = request.Payment.ReferenceNumber;
            payment.PaymentDetail.CardHolderName = request.Payment.CardHolderName;
            payment.PaymentDetail.CardLast4 = request.Payment.CardLast4;

            if (entity.Payment == null)
            {
                entity.Payment = payment;
                entity.PaymentId = payment.Id;
            }
        }

        // ShippingDetail
        if (request.ShippingDetail != null)
        {
            var shipping = entity.ShippingDetail ?? new ShippingDetail();

            shipping.FirstName = request.ShippingDetail.FirstName;
            shipping.LastName = request.ShippingDetail.LastName;
            shipping.Email = request.ShippingDetail.Email;
            shipping.PhoneNumber = request.ShippingDetail.PhoneNumber;
            shipping.Street = request.ShippingDetail.Street;
            shipping.Number = request.ShippingDetail.Number;
            shipping.Neighborhood = request.ShippingDetail.Neighborhood;
            shipping.Complement = request.ShippingDetail.Complement;
            shipping.City = request.ShippingDetail.City;
            shipping.State = request.ShippingDetail.State;
            shipping.PostCode = request.ShippingDetail.PostCode;

            if (entity.ShippingDetail == null)
            {
                entity.ShippingDetail = shipping;
                shipping.OrderId = entity.Id;
            }
        }

        if (request.OrderDetails != null)
        {
            foreach (var currentItem in entity.OrderDetails.Where(x => !x.IsDeleted).ToList())
            {
                _orderDetailRepository.Delete(currentItem);
            }

            foreach (var item in request.OrderDetails.Where(x => !string.IsNullOrWhiteSpace(x.ProductId)))
            {
                var product = await _context.Product
                    .AsNoTracking()
                    .SingleOrDefaultAsync(x => x.Id == item.ProductId, cancellationToken);

                if (product == null)
                {
                    throw new Exception($"Product not found: {item.ProductId}");
                }

                if (product.ProductAvailable == false)
                {
                    throw new ProductUnavailableException("Este produto ja foi vendido e nao pode ser adicionado ao pedido.");
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
        }

        entity.TotalAmount = await CalculateTotalAsync(entity, shippingBox, cancellationToken);
        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                if (!wasPaid && entity.Status == OrderStatus.Paid)
                {
                    await MarkProductsUnavailableWhenPaidAsync(entity, cancellationToken);
                }

                _repository.Update(entity);
                await _unitOfWork.SaveAsync(cancellationToken);
                return true;
            }, IsolationLevel.Serializable, cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ProductUnavailableException(
                "Um dos produtos foi vendido por outro pedido enquanto este pedido era salvo.", ex);
        }
        catch (Exception ex) when (ProductUnavailableException.IsDatabaseConcurrencyFailure(ex))
        {
            throw new ProductUnavailableException(
                "Um dos produtos foi vendido por outro pedido enquanto este pedido era salvo.", ex);
        }

        return new UpdateOrderResult
        {
            Data = entity
        };
    }

    private async Task<decimal> CalculateTotalAsync(
        Order entity,
        ShippingBox? shippingBox,
        CancellationToken cancellationToken)
    {
        var itemsTotal = entity.OrderDetails
            .Where(x => !x.IsDeleted)
            .Sum(x => x.TotalPrice ?? ((x.UnitPrice ?? 0m) * x.Quantity));
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
            if (product.ProductAvailable == false)
            {
                throw new ProductUnavailableException("Um dos produtos deste pedido ja foi vendido.");
            }

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
