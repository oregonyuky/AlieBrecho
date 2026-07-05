using System.Globalization;
using System.Text.Json;
using Application.Common.Services.InfinitePayManager;
using Infrastructure.Common;
using Microsoft.Extensions.Options;

namespace Infrastructure.InfinitePayManager;

public class InfinitePayService : IInfinitePayService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly InfinitePaySettings _settings;

    public InfinitePayService(HttpClient httpClient, IOptions<InfinitePaySettings> settings)
    {
        _settings = settings.Value;
    }

    public Task<InfinitePayCheckoutCreationResult> CreateCheckoutAsync(
        InfinitePayCreateCheckoutRequest request,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = ConfigurationPlaceholderResolver.Resolve(_settings.BaseUrl);
        var handle = ConfigurationPlaceholderResolver.Resolve(_settings.Handle);

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new InvalidOperationException("InfinitePay: BaseUrl nao configurada.");
        }

        if (string.IsNullOrWhiteSpace(handle))
        {
            throw new InvalidOperationException("InfinitePay: Handle nao configurado.");
        }

        var checkoutUrl = BuildCheckoutUrl(baseUrl, request.Handle ?? handle, request);

        return Task.FromResult(new InfinitePayCheckoutCreationResult(
            checkoutUrl,
            request.OrderNsu,
            null,
            null));
    }

    private static string BuildCheckoutUrl(string baseUrl, string handle, InfinitePayCreateCheckoutRequest request)
    {
        var endpoint = $"{baseUrl.TrimEnd('/')}/{Uri.EscapeDataString(handle)}";
        var query = new List<string>();

        AddQuery(query, "order_nsu", request.OrderNsu);
        AddQuery(query, "redirect_url", request.RedirectUrl);
        AddQuery(query, "webhook_url", request.WebhookUrl);
        AddQuery(query, "payment_method", request.PaymentMethod);

        var itemsJson = JsonSerializer.Serialize(
            request.Items.Select(item => new
            {
                name = item.Name,
                quantity = item.Quantity < 1 ? 1 : item.Quantity,
                price = item.Price.ToString("0.00", CultureInfo.InvariantCulture)
            }),
            JsonOptions);
        AddQuery(query, "items", itemsJson);

        return query.Count == 0 ? endpoint : $"{endpoint}?{string.Join('&', query)}";
    }

    private static void AddQuery(List<string> query, string name, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        query.Add($"{Uri.EscapeDataString(name)}={Uri.EscapeDataString(value)}");
    }
}
