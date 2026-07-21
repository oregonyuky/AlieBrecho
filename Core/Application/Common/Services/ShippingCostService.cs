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
        return (await CalculateQuoteAsync(shippingBox, destinationPostCode, cancellationToken)).Cost;
    }

    public async Task<ShippingCostQuote> CalculateQuoteAsync(
        ShippingBox? shippingBox,
        string? destinationPostCode,
        CancellationToken cancellationToken = default)
    {
        var fallbackCost = ShippingCostCalculator.Calculate(shippingBox);

        if (shippingBox == null || string.IsNullOrWhiteSpace(destinationPostCode))
        {
            return new ShippingCostQuote(fallbackCost, null);
        }

        var originPostCode = _shippingOriginProvider.GetOriginPostCode();
        if (string.IsNullOrWhiteSpace(originPostCode))
        {
            return new ShippingCostQuote(fallbackCost, null);
        }

        try
        {
            var quote = await _melhorEnvioService.CalculateCheapestShippingAsync(
                shippingBox,
                originPostCode,
                destinationPostCode,
                cancellationToken);

            return quote is null
                ? new ShippingCostQuote(fallbackCost, null)
                : new ShippingCostQuote(quote.Price, quote.CarrierName);
        }
        catch
        {
            return new ShippingCostQuote(fallbackCost, null);
        }
    }
}
