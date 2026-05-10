using Domain.Entities;

namespace Application.Common.Services;

public interface IShippingCostService
{
    Task<decimal> CalculateAsync(
        ShippingBox? shippingBox,
        string? destinationPostCode,
        CancellationToken cancellationToken = default);
}
