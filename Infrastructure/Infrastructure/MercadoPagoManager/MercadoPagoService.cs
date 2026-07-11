using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Common.Services.MercadoPagoManager;
using Microsoft.Extensions.Options;

namespace Infrastructure.MercadoPagoManager;

public sealed class MercadoPagoService : IMercadoPagoService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _httpClient;
    private readonly MercadoPagoSettings _settings;

    public MercadoPagoService(HttpClient httpClient, IOptions<MercadoPagoSettings> settings)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
    }

    public async Task<MercadoPagoPixPaymentResult> CreatePixPaymentAsync(
        MercadoPagoPixPaymentRequest request,
        CancellationToken cancellationToken)
    {
        ConfigureClient();

        var payload = new
        {
            transaction_amount = request.TransactionAmount,
            description = request.Description,
            payment_method_id = "pix",
            external_reference = request.ExternalReference,
            notification_url = request.NotificationUrl,
            date_of_expiration = FormatMercadoPagoDate(request.DateOfExpiration),
            payer = new
            {
                email = request.Payer.Email,
                first_name = request.Payer.FirstName,
                last_name = request.Payer.LastName,
                identification = new
                {
                    type = request.Payer.IdentificationType,
                    number = request.Payer.IdentificationNumber
                },
                address = request.Payer.Address is null
                    ? null
                    : new
                    {
                        zip_code = request.Payer.Address.ZipCode,
                        street_name = request.Payer.Address.StreetName,
                        street_number = request.Payer.Address.StreetNumber,
                        neighborhood = request.Payer.Address.Neighborhood,
                        city = request.Payer.Address.City,
                        federal_unit = request.Payer.Address.FederalUnit
                    }
            }
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "v1/payments")
        {
            Content = JsonContent.Create(payload, options: JsonOptions)
        };
        httpRequest.Headers.Add("X-Idempotency-Key", Guid.NewGuid().ToString("N"));

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var body = await ReadResponseBodyAsync(response, cancellationToken);
        var payment = DeserializeResponse<MercadoPagoCreatePaymentResponse>(body);
        var qrCodeBase64 = payment.PointOfInteraction?.TransactionData?.QrCodeBase64
            ?? ExtractString(body, "point_of_interaction", "transaction_data", "qr_code_base64");
        var qrCode = payment.PointOfInteraction?.TransactionData?.QrCode
            ?? ExtractString(body, "point_of_interaction", "transaction_data", "qr_code");

        if (string.IsNullOrWhiteSpace(qrCodeBase64) && string.IsNullOrWhiteSpace(qrCode))
        {
            throw new InvalidOperationException(
                $"Mercado Pago nao retornou dados Pix. Status: {payment.Status ?? "desconhecido"}. Resposta: {TrimBody(body)}");
        }

        return new MercadoPagoPixPaymentResult(
            payment.Id?.ToString(),
            payment.Status,
            qrCodeBase64,
            qrCode,
            payment.DateOfExpiration,
            payment.TransactionAmount);
    }

    public async Task<MercadoPagoPaymentStatusResult> GetPaymentAsync(
        string paymentId,
        CancellationToken cancellationToken)
    {
        ConfigureClient();

        using var response = await _httpClient.GetAsync($"v1/payments/{Uri.EscapeDataString(paymentId)}", cancellationToken);
        var body = await ReadResponseBodyAsync(response, cancellationToken);
        var payment = DeserializeResponse<MercadoPagoCreatePaymentResponse>(body);

        return new MercadoPagoPaymentStatusResult(
            payment.Id?.ToString(),
            payment.Status,
            payment.StatusDetail,
            payment.ExternalReference,
            payment.TransactionAmount,
            payment.DateApproved,
            payment.DateOfExpiration);
    }

    private void ConfigureClient()
    {
        var accessToken = Infrastructure.Common.ConfigurationPlaceholderResolver.Resolve(_settings.AccessToken);
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new InvalidOperationException("MercadoPago: MERCADO_PAGO_ACCESS_TOKEN nao configurado.");
        }

        _httpClient.BaseAddress ??= new Uri(_settings.BaseUrl ?? "https://api.mercadopago.com");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    private static async Task<string> ReadResponseBodyAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Mercado Pago retornou HTTP {(int)response.StatusCode}: {body}");
        }

        return body;
    }

    private static T DeserializeResponse<T>(string body)
    {
        return JsonSerializer.Deserialize<T>(body, JsonOptions)
            ?? throw new InvalidOperationException("Mercado Pago retornou uma resposta vazia.");
    }

    private static string? ExtractString(string body, params string[] path)
    {
        using var document = JsonDocument.Parse(body);
        var current = document.RootElement;
        foreach (var propertyName in path)
        {
            if (current.ValueKind != JsonValueKind.Object ||
                !current.TryGetProperty(propertyName, out current))
            {
                return null;
            }
        }

        return current.ValueKind == JsonValueKind.String ? current.GetString() : current.ToString();
    }

    private static string TrimBody(string body)
    {
        return body.Length <= 1000 ? body : body[..1000];
    }

    private static string? FormatMercadoPagoDate(DateTime? date)
    {
        if (date is null)
        {
            return null;
        }

        var offset = date.Value.Kind == DateTimeKind.Utc
            ? new DateTimeOffset(date.Value).ToOffset(TimeSpan.FromHours(-3))
            : new DateTimeOffset(DateTime.SpecifyKind(date.Value, DateTimeKind.Unspecified), TimeSpan.FromHours(-3));

        return offset.ToString("yyyy-MM-dd'T'HH:mm:ss.fffzzz", CultureInfo.InvariantCulture);
    }
}
