using System.Text.Json;
using System.Data;
using Application.Common;
using Application.Common.Repositories;
using Application.Common.Services.MercadoPagoManager;
using Application.Common.Services;
using ASPNET.BackEnd.Hubs;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.MercadoPagoManager;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ASPNET.BackEnd.Controllers;

[ApiController]
[Route("api/pix")]
public class PixController : ControllerBase
{
    private const string ProviderName = "MercadoPago";

    private readonly ICommandRepository<Order> _orderRepository;
    private readonly ICommandRepository<Bag> _bagRepository;
    private readonly ICommandRepository<Payment> _paymentRepository;
    private readonly ICommandRepository<Product> _productRepository;
    private readonly ICommandRepository<ShippingBox> _shippingBoxRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMercadoPagoService _mercadoPagoService;
    private readonly MercadoPagoSettings _settings;
    private readonly IHubContext<OrderNotificationsHub> _orderNotifications;
    private readonly IHubContext<CatalogNotificationsHub> _catalogNotifications;

    public PixController(
        ICommandRepository<Order> orderRepository,
        ICommandRepository<Bag> bagRepository,
        ICommandRepository<Payment> paymentRepository,
        ICommandRepository<Product> productRepository,
        ICommandRepository<ShippingBox> shippingBoxRepository,
        IUnitOfWork unitOfWork,
        IMercadoPagoService mercadoPagoService,
        IOptions<MercadoPagoSettings> settings,
        IHubContext<OrderNotificationsHub> orderNotifications,
        IHubContext<CatalogNotificationsHub> catalogNotifications)
    {
        _orderRepository = orderRepository;
        _bagRepository = bagRepository;
        _paymentRepository = paymentRepository;
        _productRepository = productRepository;
        _shippingBoxRepository = shippingBoxRepository;
        _unitOfWork = unitOfWork;
        _mercadoPagoService = mercadoPagoService;
        _settings = settings.Value;
        _orderNotifications = orderNotifications;
        _catalogNotifications = catalogNotifications;
    }

    [Authorize]
    [HttpPost("criar-pagamento")]
    public async Task<ActionResult<PixCreatePaymentResponse>> CreatePaymentAsync(
        PixCreatePaymentRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.OrderId))
        {
            return BadRequest("Pedido nao informado.");
        }

        var order = await GetOrderAsync(request.OrderId, cancellationToken);
        if (order is null)
        {
            return NotFound("Pedido nao encontrado.");
        }

        if (order.Status == OrderStatus.Paid || order.Payment?.Status == PaymentStatus.Paid)
        {
            return BadRequest("Este pedido ja esta pago.");
        }

        var amount = Math.Round(order.TotalAmount ?? 0m, 2);
        if (amount <= 0)
        {
            return BadRequest("O valor do pedido precisa ser maior que zero.");
        }

        var payer = BuildPayer(order, request);
        var payerValidation = ValidatePayer(payer);
        if (payerValidation is not null)
        {
            return BadRequest(payerValidation);
        }

        var expiration = DateTime.UtcNow.AddMinutes(Math.Max(_settings.ExpirationMinutes, 5));
        var payment = await _mercadoPagoService.CreatePixPaymentAsync(
            new MercadoPagoPixPaymentRequest
            {
                TransactionAmount = amount,
                Description = string.IsNullOrWhiteSpace(request.Description)
                    ? $"Pedido {order.Id}"
                    : request.Description,
                ExternalReference = order.Id,
                NotificationUrl = BuildWebhookUrl(),
                DateOfExpiration = expiration,
                Payer = payer
            },
            cancellationToken);

        EnsurePayment(order);
        order.Status = OrderStatus.Pending;
        order.Payment!.Status = PaymentStatus.WaitingPayment;
        order.Payment.Provider = ProviderName;
        order.Payment.CheckoutUrl = null;
        order.Payment.ProviderTransactionId = payment.PaymentId;
        order.Payment.PixQrCodeBase64 = payment.QrCodeBase64;
        order.Payment.PixQrCode = payment.QrCode;
        order.Payment.ExpiresAt = payment.DateOfExpiration ?? expiration;
        order.Payment.Amount = amount;
        order.Payment.Description = string.IsNullOrWhiteSpace(request.Description)
            ? order.Payment.Description
            : request.Description;
        EnsurePaymentDetail(order.Payment, "Pix");

        _orderRepository.Update(order);
        await _unitOfWork.SaveAsync(cancellationToken);
        await NotifyOrderChangedAsync("pix-created", order.Id, order.Status.ToString(), cancellationToken);

        return Ok(new PixCreatePaymentResponse
        {
            PaymentId = payment.PaymentId,
            QrCodeBase64 = payment.QrCodeBase64,
            QrCode = payment.QrCode,
            Status = payment.Status,
            Expiracao = payment.DateOfExpiration ?? expiration
        });
    }

    [Authorize]
    [HttpGet("status/{paymentId}")]
    public async Task<ActionResult<PixPaymentStatusResponse>> GetStatusAsync(
        string paymentId,
        CancellationToken cancellationToken)
    {
        var order = await GetOrderByPaymentIdAsync(paymentId, cancellationToken);
        if (order is null)
        {
            var bagPayment = await _mercadoPagoService.GetPaymentAsync(paymentId, cancellationToken);
            var bag = await ApplyBagPaymentStatusAsync(bagPayment, cancellationToken);
            if (bag is null)
            {
                return NotFound("Pagamento nao encontrado.");
            }

            await NotifyBagChangedAsync("pix-status-updated", bag.Id, bag.Status.ToString(), cancellationToken);

            return Ok(new PixPaymentStatusResponse
            {
                PaymentId = bagPayment.PaymentId,
                Status = bagPayment.Status,
                StatusDetail = bagPayment.StatusDetail,
                Expiracao = bagPayment.DateOfExpiration,
                OrderStatus = bag.Status.ToString()
            });
        }

        var payment = await _mercadoPagoService.GetPaymentAsync(paymentId, cancellationToken);
        var orderChanged = await ApplyPaymentStatusAsync(order, payment, cancellationToken);
        if (orderChanged)
        {
            await NotifyOrderChangedAsync("pix-status-updated", order.Id, order.Status.ToString(), cancellationToken);
        }

        return Ok(new PixPaymentStatusResponse
        {
            PaymentId = payment.PaymentId,
            Status = payment.Status,
                StatusDetail = payment.StatusDetail,
                Expiracao = payment.DateOfExpiration,
                QrCodeBase64 = payment.QrCodeBase64,
                QrCode = payment.QrCode,
                OrderStatus = order.Status.ToString()
        });
    }

    [AllowAnonymous]
    [HttpPost("/api/webhooks/mercadopago")]
    public async Task<IActionResult> MercadoPagoWebhookAsync(
        [FromBody] JsonElement payload,
        CancellationToken cancellationToken)
    {
        var paymentId = ExtractPaymentId(payload);
        if (string.IsNullOrWhiteSpace(paymentId))
        {
            paymentId = Request.Query["data.id"].FirstOrDefault()
                ?? Request.Query["id"].FirstOrDefault();
        }

        if (string.IsNullOrWhiteSpace(paymentId))
        {
            return Ok();
        }

        var order = await GetOrderByPaymentIdAsync(paymentId, cancellationToken);
        if (order is null)
        {
            var bagPayment = await _mercadoPagoService.GetPaymentAsync(paymentId, cancellationToken);
            var bag = await ApplyBagPaymentStatusAsync(bagPayment, cancellationToken);
            if (bag is not null)
            {
                await NotifyBagChangedAsync("pix-webhook-updated", bag.Id, bag.Status.ToString(), cancellationToken);
            }
            return Ok();
        }

        var payment = await _mercadoPagoService.GetPaymentAsync(paymentId, cancellationToken);
        var orderChanged = await ApplyPaymentStatusAsync(order, payment, cancellationToken);
        if (orderChanged)
        {
            await NotifyOrderChangedAsync("pix-webhook-updated", order.Id, order.Status.ToString(), cancellationToken);
        }

        return Ok();
    }

    private async Task<bool> ApplyPaymentStatusAsync(
        Order order,
        MercadoPagoPaymentStatusResult mercadoPagoPayment,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var originalOrderStatus = order.Status;
                var originalPaymentStatus = order.Payment?.Status;

                if (originalOrderStatus == OrderStatus.Paid && IsApprovedStatus(mercadoPagoPayment.Status))
                {
                    return false;
                }

                EnsurePayment(order);
                var payment = order.Payment!;
                payment.Provider = ProviderName;
                payment.ProviderTransactionId = mercadoPagoPayment.PaymentId ?? payment.ProviderTransactionId;
                payment.ExpiresAt = mercadoPagoPayment.DateOfExpiration ?? payment.ExpiresAt;
                payment.PixQrCodeBase64 = mercadoPagoPayment.QrCodeBase64 ?? payment.PixQrCodeBase64;
                payment.PixQrCode = mercadoPagoPayment.QrCode ?? payment.PixQrCode;

                if (IsApprovedStatus(mercadoPagoPayment.Status))
                {
                    var expectedAmount = Math.Round(order.TotalAmount ?? 0m, 2);
                    var paidAmount = Math.Round(mercadoPagoPayment.TransactionAmount ?? 0m, 2);
                    if (expectedAmount <= 0 || expectedAmount != paidAmount)
                    {
                        throw new InvalidOperationException("Valor recebido no Mercado Pago nao confere com o valor esperado do pedido.");
                    }

                    var paidAt = mercadoPagoPayment.DateApproved ?? DateTime.UtcNow;
                    order.Status = OrderStatus.Paid;
                    payment.Status = PaymentStatus.Paid;
                    payment.Amount = paidAmount;
                    payment.PaidAt = paidAt;
                    payment.PaymentDateTime = paidAt;
                    EnsurePaymentDetail(payment, "Pix");
                    payment.PaymentDetail!.TransactionId = payment.ProviderTransactionId;
                    payment.PaymentDetail.CapturedAt = paidAt;

                    await MarkProductsUnavailableAsync(order, cancellationToken);
                    await DeductShippingBoxStockAsync(order, cancellationToken);
                }
                else if (IsExpiredOrCancelledStatus(mercadoPagoPayment.Status))
                {
                    payment.Status = PaymentStatus.Cancelled;
                    EnsurePaymentDetail(payment, "Pix");
                }
                else
                {
                    payment.Status = PaymentStatus.WaitingPayment;
                    EnsurePaymentDetail(payment, "Pix");
                }

                _orderRepository.Update(order);
                await _unitOfWork.SaveAsync(cancellationToken);

                return originalOrderStatus != order.Status || originalPaymentStatus != payment.Status;
            }, IsolationLevel.Serializable, cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ProductUnavailableException(
                "O produto ja foi vendido por outro pagamento. Este pagamento precisa ser analisado para estorno.", ex);
        }
        catch (Exception ex) when (IsPaymentConcurrencyFailure(ex))
        {
            throw new ProductUnavailableException(
                "O produto ja foi vendido por outro pagamento. Este pagamento precisa ser analisado para estorno.", ex);
        }
    }

    private async Task DeductShippingBoxStockAsync(Order order, CancellationToken cancellationToken)
    {
        if (order.ShippingBoxStockDeducted || string.IsNullOrWhiteSpace(order.ShippingBoxId))
        {
            return;
        }

        var box = await _shippingBoxRepository.GetQuery()
            .SingleOrDefaultAsync(x => x.Id == order.ShippingBoxId && !x.IsDeleted, cancellationToken);
        if (box is null)
        {
            throw new InvalidOperationException("A embalagem selecionada nao foi encontrada.");
        }

        if (PackageStockService.DeductOnce(order, box))
        {
            _shippingBoxRepository.Update(box);
        }
    }

    private Task NotifyOrderChangedAsync(
        string changeType,
        string? orderId,
        string? status,
        CancellationToken cancellationToken)
    {
        return _orderNotifications.Clients.All.SendAsync(
            "OrderChanged",
            new
            {
                changeType,
                orderId,
                status,
                changedAt = DateTime.UtcNow
            },
            cancellationToken);
    }

    private Task NotifyBagChangedAsync(
        string changeType,
        string? bagId,
        string? status,
        CancellationToken cancellationToken)
    {
        return _orderNotifications.Clients.All.SendAsync(
            "BagChanged",
            new
            {
                changeType,
                bagId,
                status,
                changedAt = DateTime.UtcNow
            },
            cancellationToken);
    }

    private async Task<Bag?> ApplyBagPaymentStatusAsync(
        MercadoPagoPaymentStatusResult mercadoPagoPayment,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(mercadoPagoPayment.ExternalReference))
        {
            return null;
        }

        List<string> unavailableProductIds = [];
        Bag? bag;
        try
        {
            bag = await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var currentBag = await _bagRepository.GetQuery()
                    .Include(x => x.Items)
                    .SingleOrDefaultAsync(
                        x => x.Id == mercadoPagoPayment.ExternalReference && !x.IsDeleted,
                        cancellationToken);

                if (currentBag is null)
                {
                    return null;
                }

                if (IsApprovedStatus(mercadoPagoPayment.Status))
                {
                    var paidAt = mercadoPagoPayment.DateApproved ?? DateTime.UtcNow;
                    var payableItems = currentBag.Items?
                        .Where(x => !x.IsDeleted &&
                            !x.IsPaid &&
                            x.IsReserved &&
                            (!x.ReservationExpiresAt.HasValue || x.ReservationExpiresAt.Value >= paidAt))
                        .ToList() ?? [];
                    if (payableItems.Count == 0 && currentBag.Items?.Any(x => !x.IsDeleted && x.IsPaid) == true)
                    {
                        return currentBag;
                    }

                    var expectedAmount = Math.Round(payableItems.Sum(x => x.Price * x.Quantity), 2);
                    var paidAmount = Math.Round(mercadoPagoPayment.TransactionAmount ?? 0m, 2);
                    if (expectedAmount <= 0 || expectedAmount != paidAmount)
                    {
                        throw new InvalidOperationException("Valor recebido no Mercado Pago nao confere com o valor esperado da sacolinha.");
                    }

                    unavailableProductIds = payableItems
                        .Where(x => !string.IsNullOrWhiteSpace(x.ProductId))
                        .Select(x => x.ProductId!)
                        .Distinct(StringComparer.Ordinal)
                        .ToList();
                    await MarkBagProductsUnavailableAsync(unavailableProductIds, cancellationToken);

                    foreach (var item in payableItems)
                    {
                        item.IsPaid = true;
                        item.IsReserved = false;
                        item.PaidAt = paidAt;
                        item.ReservationExpiresAt = null;
                    }

                    currentBag.AllItemsPaid = currentBag.Items?.Where(x => !x.IsDeleted).All(x => x.IsPaid) ?? false;
                    currentBag.LastInteractionAt = DateTime.UtcNow;
                    currentBag.UpdatedAtUtc = DateTime.UtcNow;
                    ClearCurrentBagPayment(currentBag);
                }
                else if (IsExpiredOrCancelledStatus(mercadoPagoPayment.Status))
                {
                    ClearCurrentBagPayment(currentBag);
                }

                _bagRepository.Update(currentBag);
                await _unitOfWork.SaveAsync(cancellationToken);
                return currentBag;
            }, IsolationLevel.Serializable, cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ProductUnavailableException(
                "O produto ja foi vendido por outro pagamento. Este pagamento precisa ser analisado para estorno.", ex);
        }
        catch (Exception ex) when (IsPaymentConcurrencyFailure(ex))
        {
            throw new ProductUnavailableException(
                "O produto ja foi vendido por outro pagamento. Este pagamento precisa ser analisado para estorno.", ex);
        }

        await NotifyProductsUnavailableAsync(unavailableProductIds, cancellationToken);
        return bag;
    }

    private static void ClearCurrentBagPayment(Bag bag)
    {
        bag.CurrentPaymentId = null;
        bag.CurrentPaymentProvider = null;
        bag.CurrentPaymentQrCodeBase64 = null;
        bag.CurrentPaymentQrCode = null;
        bag.CurrentPaymentExpiresAt = null;
    }

    private async Task<Order?> GetOrderAsync(string orderId, CancellationToken cancellationToken)
    {
        return await _orderRepository.GetQuery()
            .Include(x => x.Customer)
            .Include(x => x.ShippingDetail)
            .Include(x => x.Payment)
                .ThenInclude(x => x!.PaymentDetail)
            .Include(x => x.OrderDetails)
                .ThenInclude(x => x.Product)
            .SingleOrDefaultAsync(x => x.Id == orderId && !x.IsDeleted, cancellationToken);
    }

    private async Task<Order?> GetOrderByPaymentIdAsync(string paymentId, CancellationToken cancellationToken)
    {
        return await _orderRepository.GetQuery()
            .Include(x => x.Payment)
                .ThenInclude(x => x!.PaymentDetail)
            .Include(x => x.OrderDetails)
                .ThenInclude(x => x.Product)
            .SingleOrDefaultAsync(
                x => x.Payment != null &&
                    x.Payment.Provider == ProviderName &&
                    x.Payment.ProviderTransactionId == paymentId &&
                    !x.IsDeleted,
                cancellationToken);
    }

    private void EnsurePayment(Order order)
    {
        if (order.Payment is not null)
        {
            return;
        }

        order.Payment = new Payment
        {
            Name = $"Payment for order {order.Id}",
            Amount = order.TotalAmount,
            Status = PaymentStatus.Pending,
            Provider = ProviderName,
            PaymentDetail = new PaymentDetail
            {
                PaymentMethod = "Pix"
            }
        };
        order.PaymentId = order.Payment.Id;
        order.Payment.PaymentDetail.PaymentId = order.Payment.Id;
        _paymentRepository.Create(order.Payment);
    }

    private static void EnsurePaymentDetail(Payment payment, string paymentMethod)
    {
        payment.PaymentDetail ??= new PaymentDetail
        {
            PaymentId = payment.Id
        };

        payment.PaymentDetail.PaymentMethod = paymentMethod;
    }

    private static MercadoPagoPayerRequest BuildPayer(Order order, PixCreatePaymentRequest request)
    {
        var shipping = order.ShippingDetail;
        var customer = order.Customer;
        var firstName = FirstValue(request.PayerFirstName, shipping?.FirstName);
        var lastName = FirstValue(request.PayerLastName, shipping?.LastName);

        if (string.IsNullOrWhiteSpace(firstName) && !string.IsNullOrWhiteSpace(customer?.Name))
        {
            var parts = customer.Name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            firstName = parts.FirstOrDefault();
            lastName = string.IsNullOrWhiteSpace(lastName) && parts.Length > 1
                ? string.Join(' ', parts.Skip(1))
                : lastName;
        }

        return new MercadoPagoPayerRequest
        {
            Email = FirstValue(request.PayerEmail, shipping?.Email, customer?.EmailAddress),
            FirstName = firstName,
            LastName = lastName,
            IdentificationType = "CPF",
            IdentificationNumber = OnlyDigits(FirstValue(request.PayerCpf, customer?.Cpf)),
            Address = new MercadoPagoAddressRequest
            {
                ZipCode = OnlyDigits(shipping?.PostCode ?? customer?.PostalCode),
                StreetName = FirstValue(shipping?.Street, customer?.Street),
                StreetNumber = FirstValue(shipping?.Number, customer?.Number),
                Neighborhood = FirstValue(shipping?.Neighborhood, customer?.Neighborhood),
                City = FirstValue(shipping?.City, customer?.City),
                FederalUnit = FirstValue(shipping?.State, customer?.State)
            }
        };
    }

    private static string? ValidatePayer(MercadoPagoPayerRequest payer)
    {
        if (string.IsNullOrWhiteSpace(payer.Email))
        {
            return "Informe o e-mail do comprador.";
        }

        if (string.IsNullOrWhiteSpace(payer.FirstName))
        {
            return "Informe o nome do comprador.";
        }

        if (string.IsNullOrWhiteSpace(payer.IdentificationNumber) || payer.IdentificationNumber.Length != 11)
        {
            return "Informe um CPF valido para gerar o Pix no Mercado Pago.";
        }

        return null;
    }

    private async Task MarkProductsUnavailableAsync(Order order, CancellationToken cancellationToken)
    {
        var productIds = order.OrderDetails
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
                throw new ProductUnavailableException(
                    "O produto ja foi vendido por outro pagamento. Este pagamento precisa ser analisado para estorno.");
            }

            product.ProductAvailable = false;
            _productRepository.Update(product);
        }
    }

    private async Task MarkBagProductsUnavailableAsync(
        IReadOnlyCollection<string> productIds,
        CancellationToken cancellationToken)
    {
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
                throw new ProductUnavailableException(
                    "O produto ja foi vendido por outro pagamento. Este pagamento precisa ser analisado para estorno.");
            }

            product.ProductAvailable = false;
            _productRepository.Update(product);
        }
    }

    private async Task NotifyProductsUnavailableAsync(
        IEnumerable<string> productIds,
        CancellationToken cancellationToken)
    {
        foreach (var productId in productIds)
        {
            await _catalogNotifications.Clients.All.SendAsync(
                "ProductChanged",
                new
                {
                    changeType = "sold",
                    productId,
                    productAvailable = false,
                    changedAt = DateTime.UtcNow
                },
                cancellationToken);
        }
    }

    private static bool IsApprovedStatus(string? status)
    {
        return string.Equals(status, "approved", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPaymentConcurrencyFailure(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException!)
        {
            if (current.Message.Contains("1205", StringComparison.OrdinalIgnoreCase)
                || current.Message.Contains("40001", StringComparison.OrdinalIgnoreCase)
                || current.Message.Contains("40P01", StringComparison.OrdinalIgnoreCase)
                || current.Message.Contains("deadlock", StringComparison.OrdinalIgnoreCase)
                || current.Message.Contains("database is locked", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsExpiredOrCancelledStatus(string? status)
    {
        return string.Equals(status, "cancelled", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(status, "expired", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(status, "rejected", StringComparison.OrdinalIgnoreCase);
    }

    private static string? ExtractPaymentId(JsonElement payload)
    {
        if (payload.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (payload.TryGetProperty("data", out var data) &&
            data.ValueKind == JsonValueKind.Object &&
            data.TryGetProperty("id", out var dataId))
        {
            return dataId.ToString();
        }

        if (payload.TryGetProperty("id", out var id))
        {
            return id.ToString();
        }

        return null;
    }

    private string? BuildWebhookUrl()
    {
        if (!Request.Host.HasValue ||
            string.Equals(Request.Host.Host, "localhost", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Request.Host.Host, "127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(Request.Scheme, "https", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return $"{Request.Scheme}://{Request.Host}/api/webhooks/mercadopago";
    }

    private static string? FirstValue(params string?[] values)
    {
        return values.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))?.Trim();
    }

    private static string? OnlyDigits(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var digits = new string(value.Where(char.IsDigit).ToArray());
        return string.IsNullOrWhiteSpace(digits) ? null : digits;
    }
}

public sealed record PixCreatePaymentRequest
{
    public string? OrderId { get; init; }
    public string? Description { get; init; }
    public string? PayerFirstName { get; init; }
    public string? PayerLastName { get; init; }
    public string? PayerEmail { get; init; }
    public string? PayerCpf { get; init; }
}

public sealed record PixCreatePaymentResponse
{
    public string? PaymentId { get; init; }
    public string? QrCodeBase64 { get; init; }
    public string? QrCode { get; init; }
    public string? Status { get; init; }
    public DateTime? Expiracao { get; init; }
}

public sealed record PixPaymentStatusResponse
{
    public string? PaymentId { get; init; }
    public string? Status { get; init; }
    public string? StatusDetail { get; init; }
    public DateTime? Expiracao { get; init; }
    public string? QrCodeBase64 { get; init; }
    public string? QrCode { get; init; }
    public string? OrderStatus { get; init; }
}
