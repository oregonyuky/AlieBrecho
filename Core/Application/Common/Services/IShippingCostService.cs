using Domain.Entities;

namespace Application.Common.Services;

public sealed record ShippingCostQuote(decimal Cost, string? CarrierName);

public interface IShippingCostService
{
    Task<decimal> CalculateAsync(
        ShippingBox? shippingBox,
        string? destinationPostCode,
        CancellationToken cancellationToken = default);
    async Task<ShippingCostQuote> CalculateQuoteAsync(
        ShippingBox? shippingBox,
        string? destinationPostCode,
        CancellationToken cancellationToken = default) =>
        new(await CalculateAsync(shippingBox, destinationPostCode, cancellationToken), null);
}
