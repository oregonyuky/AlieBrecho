using Application.Common.Services.MelhorEnvioManager;
using Domain.Entities;

namespace Application.Common.Services;

public class ShippingCostService : IShippingCostService
{
    private readonly IMelhorEnvioService _melhorEnvioService;
    private readonly IShippingOriginProvider _shippingOriginProvider;

    public ShippingCostService(
        IMelhorEnvioService melhorEnvioService,
        IShippingOriginProvider shippingOriginProvider)
    {
        _melhorEnvioService = melhorEnvioService;
        _shippingOriginProvider = shippingOriginProvider;
    }

    public async Task<decimal> CalculateAsync(
        ShippingBox? shippingBox,
        string? destinationPostCode,
        CancellationToken cancellationToken = default)
    {
        var fallbackCost = ShippingCostCalculator.Calculate(shippingBox);

        if (shippingBox == null || string.IsNullOrWhiteSpace(destinationPostCode))
        {
            return fallbackCost;
        }

        var originPostCode = _shippingOriginProvider.GetOriginPostCode();
        if (string.IsNullOrWhiteSpace(originPostCode))
        {
            return fallbackCost;
        }

        try
        {
            var shippingCost = await _melhorEnvioService.CalculateShippingCostAsync(
                shippingBox,
                originPostCode,
                destinationPostCode,
                cancellationToken);

            return shippingCost ?? fallbackCost;
        }
        catch
        {
            return fallbackCost;
        }
    }
}
