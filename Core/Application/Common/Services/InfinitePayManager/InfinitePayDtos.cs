using System.Text.Json.Serialization;

namespace Application.Common.Services.InfinitePayManager;

public sealed record InfinitePayCreateCheckoutRequest
{
    [JsonPropertyName("handle")]
    public string? Handle { get; init; }

    [JsonPropertyName("order_nsu")]
    public string? OrderNsu { get; init; }

    [JsonPropertyName("redirect_url")]
    public string? RedirectUrl { get; init; }

    [JsonPropertyName("webhook_url")]
    public string? WebhookUrl { get; init; }

    [JsonPropertyName("payment_method")]
    public string? PaymentMethod { get; init; }

    [JsonPropertyName("items")]
    public IReadOnlyList<InfinitePayItemRequest> Items { get; init; } = [];
}

public sealed record InfinitePayItemRequest
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("quantity")]
    public int Quantity { get; init; }

    [JsonPropertyName("price")]
    public decimal Price { get; init; }
}

public sealed record InfinitePayCreateCheckoutResponse
{
    [JsonPropertyName("payment_url")]
    public string? PaymentUrl { get; init; }

    [JsonPropertyName("checkout_url")]
    public string? CheckoutUrl { get; init; }

    [JsonPropertyName("url")]
    public string? Url { get; init; }

    [JsonPropertyName("transaction_id")]
    public string? TransactionId { get; init; }

    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("pix_qr_code")]
    public string? PixQrCode { get; init; }

    [JsonPropertyName("qr_code")]
    public string? QrCode { get; init; }

    [JsonPropertyName("pix_code")]
    public string? PixCode { get; init; }

    [JsonPropertyName("br_code")]
    public string? BrCode { get; init; }

    [JsonPropertyName("copy_paste")]
    public string? CopyPaste { get; init; }
}

public sealed record InfinitePayCheckoutCreationResult(
    string? PaymentUrl,
    string? ProviderTransactionId,
    string? PixQrCode,
    string? PixCode);

public sealed record InfinitePayWebhookRequest
{
    [JsonPropertyName("order_nsu")]
    public string? OrderNsu { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("transaction_id")]
    public string? TransactionId { get; init; }

    [JsonPropertyName("id")]
    public string? Id { get; init; }
}
