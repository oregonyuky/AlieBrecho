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
            throw new Exception(error);
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
}
