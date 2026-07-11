using System.Text.Json.Serialization;

namespace Application.Common.Services.MercadoPagoManager;

public sealed record MercadoPagoPixPaymentRequest
{
    public decimal TransactionAmount { get; init; }
    public string? Description { get; init; }
    public string? ExternalReference { get; init; }
    public string? NotificationUrl { get; init; }
    public DateTime? DateOfExpiration { get; init; }
    public MercadoPagoPayerRequest Payer { get; init; } = new();
}

public sealed record MercadoPagoPayerRequest
{
    public string? Email { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? IdentificationType { get; init; }
    public string? IdentificationNumber { get; init; }
    public MercadoPagoAddressRequest? Address { get; init; }
}

public sealed record MercadoPagoAddressRequest
{
    public string? ZipCode { get; init; }
    public string? StreetName { get; init; }
    public string? StreetNumber { get; init; }
    public string? Neighborhood { get; init; }
    public string? City { get; init; }
    public string? FederalUnit { get; init; }
}

public sealed record MercadoPagoPixPaymentResult(
    string? PaymentId,
    string? Status,
    string? QrCodeBase64,
    string? QrCode,
    DateTime? DateOfExpiration,
    decimal? TransactionAmount);

public sealed record MercadoPagoPaymentStatusResult(
    string? PaymentId,
    string? Status,
    string? StatusDetail,
    string? ExternalReference,
    decimal? TransactionAmount,
    DateTime? DateApproved,
    DateTime? DateOfExpiration);

public sealed record MercadoPagoCreatePaymentResponse
{
    [JsonPropertyName("id")]
    public long? Id { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("status_detail")]
    public string? StatusDetail { get; init; }

    [JsonPropertyName("transaction_amount")]
    public decimal? TransactionAmount { get; init; }

    [JsonPropertyName("external_reference")]
    public string? ExternalReference { get; init; }

    [JsonPropertyName("date_approved")]
    public DateTime? DateApproved { get; init; }

    [JsonPropertyName("date_of_expiration")]
    public DateTime? DateOfExpiration { get; init; }

    [JsonPropertyName("point_of_interaction")]
    public MercadoPagoPointOfInteractionResponse? PointOfInteraction { get; init; }
}

public sealed record MercadoPagoPointOfInteractionResponse
{
    [JsonPropertyName("transaction_data")]
    public MercadoPagoTransactionDataResponse? TransactionData { get; init; }
}

public sealed record MercadoPagoTransactionDataResponse
{
    [JsonPropertyName("qr_code_base64")]
    public string? QrCodeBase64 { get; init; }

    [JsonPropertyName("qr_code")]
    public string? QrCode { get; init; }
}
