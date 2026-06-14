using System.Net.Http.Headers;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Application.Common.Services.MelhorEnvioManager;
using Domain.Entities;
using Infrastructure.Common;

namespace Infrastructure.MelhorEnvioManager;

public class MelhorEnvioService : IMelhorEnvioService
{
    private readonly HttpClient _httpClient;
    private readonly MelhorEnvioSettings _settings;

    public MelhorEnvioService(
        HttpClient httpClient,
        IOptions<MelhorEnvioSettings> settings)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
    }

    private void AddAuth()
    {
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.DefaultRequestHeaders.UserAgent.Clear();
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("AlieBrecho (admin@root.com)");
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", ConfigurationPlaceholderResolver.Resolve(_settings.Token) ?? string.Empty);
    }

    public async Task<string> CalcularFreteAsync(object request)
    {
        AddAuth();

        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json"
        );

        var response = await _httpClient.PostAsync(
            $"{ConfigurationPlaceholderResolver.Resolve(_settings.BaseUrl)}v2/me/shipment/calculate",
            content
        );

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception(error);
        }

        return await response.Content.ReadAsStringAsync();
    }

    public async Task<string> CriarEtiquetaAsync(object request)
    {
        AddAuth();

        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json"
        );

        var response = await _httpClient.PostAsync(
            $"{ConfigurationPlaceholderResolver.Resolve(_settings.BaseUrl)}v2/me/shipment",
            content
        );

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception(error);
        }

        return await response.Content.ReadAsStringAsync();
    }

    public async Task<string> AdicionarEtiquetaAoCarrinhoAsync(object request, CancellationToken cancellationToken = default)
    {
        AddAuth();

        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json"
        );

        var response = await _httpClient.PostAsync(
            $"{ConfigurationPlaceholderResolver.Resolve(_settings.BaseUrl)}v2/me/cart",
            content,
            cancellationToken
        );

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new Exception(GetApiErrorMessage(error));
        }

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    public async Task<string> ConsultarSaldoAsync(CancellationToken cancellationToken = default)
    {
        AddAuth();

        var response = await _httpClient.GetAsync(
            $"{ConfigurationPlaceholderResolver.Resolve(_settings.BaseUrl)}v2/me/balance",
            cancellationToken
        );

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new Exception(GetApiErrorMessage(error));
        }

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    public async Task<string> ComprarFretesAsync(object request, CancellationToken cancellationToken = default)
    {
        AddAuth();

        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json"
        );

        var response = await _httpClient.PostAsync(
            $"{ConfigurationPlaceholderResolver.Resolve(_settings.BaseUrl)}v2/me/shipment/checkout",
            content,
            cancellationToken
        );

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new Exception(GetApiErrorMessage(error));
        }

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    public async Task<string> GerarEtiquetasAsync(object request, CancellationToken cancellationToken = default)
    {
        AddAuth();

        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json"
        );

        var response = await _httpClient.PostAsync(
            $"{ConfigurationPlaceholderResolver.Resolve(_settings.BaseUrl)}v2/me/shipment/generate",
            content,
            cancellationToken
        );

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new Exception(GetApiErrorMessage(error));
        }

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    public async Task<byte[]> BaixarEtiquetaPdfAsync(string labelId, CancellationToken cancellationToken = default)
    {
        const int maxAttempts = 6;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            AddAuth();
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/pdf"));

            var response = await _httpClient.GetAsync(
                $"{ConfigurationPlaceholderResolver.Resolve(_settings.BaseUrl)}v2/me/imprimir/pdf/{Uri.EscapeDataString(labelId)}",
                cancellationToken
            );

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsByteArrayAsync(cancellationToken);
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            var message = GetApiErrorMessage(error);

            if (attempt < maxAttempts && IsPdfProcessingMessage(message))
            {
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
                continue;
            }

            throw new Exception(message);
        }

        throw new Exception("Etiqueta pdf nao processada.");
    }

    public async Task<decimal?> CalculateShippingCostAsync(
        ShippingBox shippingBox,
        string originPostCode,
        string destinationPostCode,
        CancellationToken cancellationToken = default)
    {
        var request = new
        {
            from = new
            {
                postal_code = OnlyDigits(originPostCode)
            },
            to = new
            {
                postal_code = OnlyDigits(destinationPostCode)
            },
            products = new[]
            {
                new
                {
                    id = shippingBox.Id,
                    width = shippingBox.Width ?? 0m,
                    height = shippingBox.Height ?? 0m,
                    length = shippingBox.Length ?? 0m,
                    weight = shippingBox.Weight ?? 0m,
                    insurance_value = shippingBox.InsuranceValue ?? 0m,
                    quantity = 1
                }
            },
            options = new
            {
                receipt = false,
                own_hand = false
            }
        };

        var response = await CalcularFreteAsync(request, cancellationToken);
        return GetCheapestPrice(response);
    }

    private async Task<string> CalcularFreteAsync(object request, CancellationToken cancellationToken)
    {
        AddAuth();

        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json"
        );

        var response = await _httpClient.PostAsync(
            $"{ConfigurationPlaceholderResolver.Resolve(_settings.BaseUrl)}v2/me/shipment/calculate",
            content,
            cancellationToken
        );

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new Exception(GetApiErrorMessage(error));
        }

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private static decimal? GetCheapestPrice(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        decimal? cheapestPrice = null;

        foreach (var item in document.RootElement.EnumerateArray())
        {
            var price = GetDecimal(item, "custom_price") ?? GetDecimal(item, "price");
            if (price == null)
            {
                continue;
            }

            cheapestPrice = cheapestPrice == null
                ? price
                : Math.Min(cheapestPrice.Value, price.Value);
        }

        return cheapestPrice;
    }

    private static decimal? GetDecimal(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.Number when property.TryGetDecimal(out var number) => number,
            JsonValueKind.String when decimal.TryParse(property.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var number) => number,
            _ => null
        };
    }

    private static string OnlyDigits(string value)
    {
        return new string(value.Where(char.IsDigit).ToArray());
    }

    private static string GetApiErrorMessage(string responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return "Melhor Envio retornou uma resposta vazia.";
        }

        try
        {
            using var document = JsonDocument.Parse(responseBody);
            var root = document.RootElement;

            if (root.TryGetProperty("message", out var message) &&
                message.ValueKind == JsonValueKind.String &&
                !string.IsNullOrWhiteSpace(message.GetString()))
            {
                return message.GetString()!;
            }

            if (root.TryGetProperty("error", out var error) &&
                error.ValueKind == JsonValueKind.String &&
                !string.IsNullOrWhiteSpace(error.GetString()))
            {
                return error.GetString()!;
            }

            if (root.TryGetProperty("errors", out var errors))
            {
                var messages = new List<string>();
                CollectErrorMessages(errors, messages);

                if (messages.Count > 0)
                {
                    return string.Join(" ", messages.Distinct());
                }
            }
        }
        catch (JsonException)
        {
            return responseBody;
        }

        return responseBody;
    }

    private static bool IsPdfProcessingMessage(string message)
    {
        return message.Contains("E-PRT-0007", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("pdf nao processada", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("pdf não processada", StringComparison.OrdinalIgnoreCase);
    }

    private static void CollectErrorMessages(JsonElement element, List<string> messages)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                var value = element.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    messages.Add(value);
                }
                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    CollectErrorMessages(item, messages);
                }
                break;
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    CollectErrorMessages(property.Value, messages);
                }
                break;
        }
    }
}
