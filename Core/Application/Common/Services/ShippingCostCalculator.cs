using Domain.Entities;

namespace Application.Common.Services;

public static class ShippingCostCalculator
{
    private const decimal CubicWeightDivisor = 6000m;
    private const decimal PricePerKg = 10m;
    private const decimal InsuranceRate = 0.01m;

    public static decimal Calculate(ShippingBox? shippingBox)
    {
        if (shippingBox == null)
        {
            return 0m;
        }

        var weight = shippingBox.Weight ?? 0m;
        var cubicWeight = ((shippingBox.Width ?? 0m) * (shippingBox.Length ?? 0m) * (shippingBox.Height ?? 0m)) / CubicWeightDivisor;
        var chargedWeight = Math.Max(weight, cubicWeight);
        var insurance = (shippingBox.InsuranceValue ?? 0m) * InsuranceRate;

        return Math.Round((chargedWeight * PricePerKg) + insurance, 2);
    }
}
