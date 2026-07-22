using Application.Common.CQS.Queries;
using Application.Common;
using Application.Common.Repositories;
using Application.Common.Services;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Data;

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
    private readonly IAutomaticPackageSelectionService _packageSelectionService;

    public CreateOrderHandler(
        ICommandRepository<Order> repository,
        ICommandRepository<Product> productRepository,
        IUnitOfWork unitOfWork,
        IQueryContext context,
        IShippingCostService shippingCostService,
        IAutomaticPackageSelectionService packageSelectionService)
    {
        _repository = repository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _context = context;
        _shippingCostService = shippingCostService;
        _packageSelectionService = packageSelectionService;
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

        var selection = await _packageSelectionService.SelectBestPackageAsync(
            (request.OrderDetails ?? [])
                .Where(x => !string.IsNullOrWhiteSpace(x.ProductId))
                .Select(x => new PackageSelectionItem(x.ProductId!, x.Quantity))
                .ToList(),
            cancellationToken);
        var shippingBox = selection.Package;
        if (shippingBox is null)
        {
            throw new ValidationException("Nenhuma embalagem disponivel comporta os produtos deste pedido.");
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
            ShippingBoxId = shippingBox.Id,
            PackageName = shippingBox.Name,
            PackageLength = shippingBox.Length,
            PackageWidth = shippingBox.Width,
            PackageHeight = shippingBox.Height,
            PackageWeight = shippingBox.Weight,
            PackageCapacityPoints = shippingBox.PackageCategory?.CapacityPoints,
            PackageOccupationPoints = selection.TotalOccupationPoints,
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
                ProductName = product.Name,
                ProductImageUrl = product.MainImageURL ?? product.Picture1,
                Quantity = quantity,
                UnitPrice = unitPrice,
                TotalPrice = unitPrice * quantity
            });
        }

        var shippingPackage = CreateShippingPackage(shippingBox, selection.TotalProductWeight);
        entity.TotalAmount = await CalculateTotalAsync(entity, shippingPackage, cancellationToken);
        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                await MarkProductsUnavailableWhenPaidAsync(entity, cancellationToken);
                await _repository.CreateAsync(entity, cancellationToken);
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

        return new CreateOrderResult
        {
            Data = entity
        };
    }

    private static ShippingBox CreateShippingPackage(ShippingBox box, decimal productWeight) => new()
    {
        Id = box.Id,
        Name = box.Name,
        Width = box.Width,
        Length = box.Length,
        Height = box.Height,
        Weight = (box.Weight ?? 0m) + productWeight,
        InsuranceValue = box.InsuranceValue,
        PackageCategoryId = box.PackageCategoryId,
        PackageCategory = box.PackageCategory,
        StockQuantity = box.StockQuantity,
        MaxWeight = box.MaxWeight,
        IsActive = box.IsActive
    };

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
