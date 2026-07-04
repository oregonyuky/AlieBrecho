using System.Net.Http.Json;
using System.Text.Json;
using Application.Common.Services.InfinitePayManager;
using Infrastructure.Common;
using Microsoft.Extensions.Options;

namespace Infrastructure.InfinitePayManager;

public class InfinitePayService : IInfinitePayService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;
    private readonly InfinitePaySettings _settings;

    public InfinitePayService(HttpClient httpClient, IOptions<InfinitePaySettings> settings)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
    }

    public async Task<InfinitePayCheckoutCreationResult> CreateCheckoutAsync(
        InfinitePayCreateCheckoutRequest request,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = ConfigurationPlaceholderResolver.Resolve(_settings.BaseUrl);
        var handle = ConfigurationPlaceholderResolver.Resolve(_settings.Handle);
        var path = ConfigurationPlaceholderResolver.Resolve(_settings.CreateCheckoutPath) ?? "checkouts";

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new InvalidOperationException("InfinitePay: BaseUrl nao configurada.");
        }

        if (string.IsNullOrWhiteSpace(handle))
        {
            throw new InvalidOperationException("InfinitePay: Handle nao configurado.");
        }

        var endpoint = BuildEndpoint(baseUrl, path, handle);
        var payload = request.Handle is null ? request with { Handle = handle } : request;
        var response = await _httpClient.PostAsJsonAsync(endpoint, payload, JsonOptions, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Infinite Pay retornou erro {(int)response.StatusCode}: {GetApiErrorMessage(body)}");
        }

        var checkout = ReadCheckoutResponse(body);
        var paymentUrl = checkout.PaymentUrl;
        var pixQrCode = checkout.PixQrCode;
        var pixCode = checkout.PixCode;

        if (string.IsNullOrWhiteSpace(paymentUrl) && string.IsNullOrWhiteSpace(pixQrCode) && string.IsNullOrWhiteSpace(pixCode))
        {
            throw new InvalidOperationException(
                $"Infinite Pay nao retornou URL de pagamento nem dados Pix. Resposta: {TrimForMessage(body)}");
        }

        return new InfinitePayCheckoutCreationResult(
            paymentUrl,
            checkout.ProviderTransactionId,
            pixQrCode,
            pixCode);
    }

    private static string BuildEndpoint(string baseUrl, string path, string handle)
    {
        var normalizedBaseUrl = baseUrl.TrimEnd('/');
        var normalizedPath = path.Trim('/');

        return normalizedPath.Contains("{handle}", StringComparison.OrdinalIgnoreCase)
            ? $"{normalizedBaseUrl}/{normalizedPath.Replace("{handle}", Uri.EscapeDataString(handle), StringComparison.OrdinalIgnoreCase)}"
            : $"{normalizedBaseUrl}/{Uri.EscapeDataString(handle)}/{normalizedPath}";
    }

    private static string GetApiErrorMessage(string responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return "resposta vazia.";
        }

        try
        {
            using var document = JsonDocument.Parse(responseBody);
            var root = document.RootElement;

            foreach (var propertyName in new[] { "message", "error", "detail", "title" })
            {
                if (root.TryGetProperty(propertyName, out var property) &&
                    property.ValueKind == JsonValueKind.String &&
                    !string.IsNullOrWhiteSpace(property.GetString()))
                {
                    return property.GetString()!;
                }
            }
        }
        catch (JsonException)
        {
            return responseBody;
        }

        return responseBody;
    }

    private static CheckoutResponseData ReadCheckoutResponse(string body)
    {
        if (Uri.TryCreate(body.Trim().Trim('"'), UriKind.Absolute, out var directUrl))
        {
            return new CheckoutResponseData(directUrl.ToString(), null, null, null);
        }

        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;

        return new CheckoutResponseData(
            FindString(root, new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "payment_url",
                "paymentUrl",
                "checkout_url",
                "checkoutUrl",
                "url",
                "link",
                "link_url",
                "linkUrl",
                "payment_link",
                "paymentLink",
                "checkout_link",
                "checkoutLink",
                "secure_url",
                "secureUrl"
            }),
            FindString(root, new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "pix_qr_code",
                "pixQrCode",
                "qr_code",
                "qrCode",
                "qrcode",
                "qrCodeImage",
                "qr_code_image"
            }),
            FindString(root, new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "pix_code",
                "pixCode",
                "br_code",
                "brCode",
                "copy_paste",
                "copyPaste",
                "copia_e_cola",
                "copiaECopia",
                "payload"
            }),
            FindString(root, new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "transaction_id",
                "transactionId",
                "provider_transaction_id",
                "providerTransactionId",
                "charge_id",
                "chargeId",
                "id"
            }));
    }

    private static string? FindString(JsonElement element, IReadOnlySet<string> names)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    if (names.Contains(property.Name) &&
                        property.Value.ValueKind == JsonValueKind.String &&
                        !string.IsNullOrWhiteSpace(property.Value.GetString()))
                    {
                        return property.Value.GetString();
                    }
                }

                foreach (var property in element.EnumerateObject())
                {
                    var value = FindString(property.Value, names);
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        return value;
                    }
                }

                break;

            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    var value = FindString(item, names);
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        return value;
                    }
                }

                break;

        }

        return null;
    }

    private static string TrimForMessage(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return "vazia.";
        }

        return body.Length <= 800 ? body : $"{body[..800]}...";
    }

    private sealed record CheckoutResponseData(
        string? PaymentUrl,
        string? PixQrCode,
        string? PixCode,
        string? ProviderTransactionId);
}
