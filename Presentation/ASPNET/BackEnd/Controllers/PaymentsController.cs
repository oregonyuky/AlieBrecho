using Application.Common.Repositories;
using Application.Common.Services.InfinitePayManager;
using ASPNET.BackEnd.Hubs;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.InfinitePayManager;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ASPNET.BackEnd.Controllers;

[ApiController]
[Route("api/payments/infinitepay")]
public class PaymentsController : ControllerBase
{
    private const string ProviderName = "InfinitePay";
    private readonly ICommandRepository<Order> _orderRepository;
    private readonly ICommandRepository<Payment> _paymentRepository;
    private readonly ICommandRepository<Product> _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IInfinitePayService _infinitePayService;
    private readonly InfinitePaySettings _settings;
    private readonly IHubContext<OrderNotificationsHub> _orderNotifications;

    public PaymentsController(
        ICommandRepository<Order> orderRepository,
        ICommandRepository<Payment> paymentRepository,
        ICommandRepository<Product> productRepository,
        IUnitOfWork unitOfWork,
        IInfinitePayService infinitePayService,
        IOptions<InfinitePaySettings> settings,
        IHubContext<OrderNotificationsHub> orderNotifications)
    {
        _orderRepository = orderRepository;
        _paymentRepository = paymentRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _infinitePayService = infinitePayService;
        _settings = settings.Value;
        _orderNotifications = orderNotifications;
    }

    [Authorize(Policy = "UserOrCustomer")]
    [HttpPost("checkout")]
    public async Task<ActionResult<InfinitePayCheckoutResult>> CreateCheckoutAsync(
        InfinitePayCheckoutRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.OrderId))
        {
            return BadRequest("Pedido nao informado pelo checkout.");
        }

        if (!TryNormalizePaymentMethod(request.PaymentMethod, out var paymentMethod))
        {
            return BadRequest("Forma de pagamento invalida. Use 'pix' ou 'credit_card'.");
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

        var redirectUrl = ResolveSetting(_settings.RedirectUrl);
        var webhookUrl = ResolveSetting(_settings.WebhookUrl);

        var urlValidationError = ValidatePublicInfinitePayUrls(redirectUrl, webhookUrl);
        if (urlValidationError is not null)
        {
            return BadRequest(urlValidationError);
        }

        var checkoutRequest = new InfinitePayCreateCheckoutRequest
        {
            OrderNsu = order.Id,
            RedirectUrl = redirectUrl,
            WebhookUrl = webhookUrl,
            PaymentMethod = paymentMethod,
            Items = BuildItems(order)
        };

        var checkout = await _infinitePayService.CreateCheckoutAsync(checkoutRequest, cancellationToken);

        EnsurePayment(order);
        order.Status = OrderStatus.Pending;
        order.Payment!.Status = PaymentStatus.WaitingPayment;
        order.Payment.Provider = ProviderName;
        order.Payment.CheckoutUrl = checkout.PaymentUrl;
        order.Payment.ProviderTransactionId = checkout.ProviderTransactionId;
        order.Payment.Amount = order.TotalAmount;
        EnsurePaymentDetail(order.Payment, GetDisplayPaymentMethod(paymentMethod));

        _orderRepository.Update(order);
        await _unitOfWork.SaveAsync(cancellationToken);
        await NotifyOrderChangedAsync("payment-checkout-created", order.Id, order.Status.ToString(), cancellationToken);

        return Ok(new InfinitePayCheckoutResult
        {
            PaymentUrl = checkout.PaymentUrl,
            PixQrCode = checkout.PixQrCode,
            PixCode = checkout.PixCode
        });
    }

    [AllowAnonymous]
    [HttpPost("webhook")]
    public async Task<IActionResult> WebhookAsync(
        InfinitePayWebhookRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.OrderNsu))
        {
            return Ok();
        }

        var order = await GetOrderAsync(request.OrderNsu, cancellationToken);
        if (order is null)
        {
            return Ok();
        }

        if (!IsPaidStatus(request.Status))
        {
            return Ok();
        }

        EnsurePayment(order);
        var payment = order.Payment!;

        if (order.Status != OrderStatus.Paid || payment.Status != PaymentStatus.Paid)
        {
            var paidAt = DateTime.UtcNow;
            order.Status = OrderStatus.Paid;
            payment.Status = PaymentStatus.Paid;
            payment.Provider = ProviderName;
            payment.PaidAt = paidAt;
            payment.PaymentDateTime = paidAt;
            payment.ProviderTransactionId = request.TransactionId ?? request.Id ?? payment.ProviderTransactionId;

            if (payment.PaymentDetail is null)
            {
                payment.PaymentDetail = new PaymentDetail
                {
                    PaymentId = payment.Id,
                    PaymentMethod = ProviderName
                };
            }

            payment.PaymentDetail.TransactionId = payment.ProviderTransactionId;
            payment.PaymentDetail.CapturedAt = paidAt;

            await MarkProductsUnavailableAsync(order, cancellationToken);
            _orderRepository.Update(order);
            await _unitOfWork.SaveAsync(cancellationToken);
            await NotifyOrderChangedAsync("payment-paid", order.Id, order.Status.ToString(), cancellationToken);
        }

        return Ok();
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

    private async Task<Order?> GetOrderAsync(string orderId, CancellationToken cancellationToken)
    {
        return await _orderRepository.GetQuery()
            .Include(x => x.Payment)
                .ThenInclude(x => x!.PaymentDetail)
            .Include(x => x.OrderDetails)
                .ThenInclude(x => x.Product)
            .SingleOrDefaultAsync(x => x.Id == orderId && !x.IsDeleted, cancellationToken);
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
                PaymentMethod = ProviderName
            }
        };
        order.PaymentId = order.Payment.Id;
        order.Payment.PaymentDetail.PaymentId = order.Payment.Id;
        _paymentRepository.Create(order.Payment);
    }

    private static IReadOnlyList<InfinitePayItemRequest> BuildItems(Order order)
    {
        var items = order.OrderDetails
            .Where(x => !x.IsDeleted)
            .Select(x => new InfinitePayItemRequest
            {
                Name = x.Product?.Name ?? $"Produto {x.ProductId}",
                Quantity = x.Quantity < 1 ? 1 : x.Quantity,
                Price = x.UnitPrice ?? 0m
            })
            .ToList();

        if (order.Taxes is > 0)
        {
            items.Add(new InfinitePayItemRequest
            {
                Name = "Taxas",
                Quantity = 1,
                Price = order.Taxes.Value
            });
        }

        return items;
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
            product.ProductAvailable = false;
            _productRepository.Update(product);
        }
    }

    private static bool IsPaidStatus(string? status)
    {
        return string.Equals(status, "paid", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(status, "approved", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(status, "confirmed", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(status, "completed", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryNormalizePaymentMethod(string? paymentMethod, out string normalized)
    {
        normalized = paymentMethod?.Trim().ToLowerInvariant() ?? string.Empty;
        return normalized is "pix" or "credit_card";
    }

    private static string GetDisplayPaymentMethod(string paymentMethod)
    {
        return paymentMethod == "pix" ? "Pix" : "CreditCard";
    }

    private static void EnsurePaymentDetail(Payment payment, string paymentMethod)
    {
        payment.PaymentDetail ??= new PaymentDetail
        {
            PaymentId = payment.Id
        };

        payment.PaymentDetail.PaymentMethod = paymentMethod;
    }

    private static string? ResolveSetting(string? value)
    {
        return Infrastructure.Common.ConfigurationPlaceholderResolver.Resolve(value);
    }

    private static string? ValidatePublicInfinitePayUrls(string? redirectUrl, string? webhookUrl)
    {
        if (string.IsNullOrWhiteSpace(redirectUrl))
        {
            return "InfinitePay: INFINITE_PAY_REDIRECT_URL nao configurada.";
        }

        if (string.IsNullOrWhiteSpace(webhookUrl))
        {
            return "InfinitePay: INFINITE_PAY_WEBHOOK_URL nao configurada.";
        }

        if (!IsPublicHttpUrl(redirectUrl))
        {
            return "InfinitePay: INFINITE_PAY_REDIRECT_URL precisa ser uma URL publica HTTPS. Use o dominio de producao ou um tunel publico como ngrok.";
        }

        if (!IsPublicHttpUrl(webhookUrl))
        {
            return "InfinitePay: INFINITE_PAY_WEBHOOK_URL precisa ser uma URL publica HTTPS. Use o dominio de producao ou um tunel publico como ngrok.";
        }

        return null;
    }

    private static bool IsPublicHttpUrl(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return !string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(uri.Host, "127.0.0.1", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(uri.Host, "::1", StringComparison.OrdinalIgnoreCase);
    }
}

public sealed record InfinitePayCheckoutRequest
{
    public string? OrderId { get; init; }
    public string? PaymentMethod { get; init; }
}

public sealed record InfinitePayCheckoutResult
{
    public string? PaymentUrl { get; init; }
    public string? PixQrCode { get; init; }
    public string? PixCode { get; init; }
}
