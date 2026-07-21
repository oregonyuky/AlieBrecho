using Application.Common.Services;
using Domain.Entities;
using Xunit;

namespace Application.Tests;

public class PackageStockServiceTests
{
    [Fact]
    public void RepeatedPaymentNotificationDeductsStockOnlyOnce()
    {
        var order = new Order();
        var package = new ShippingBox { StockQuantity = 2 };

        Assert.True(PackageStockService.DeductOnce(order, package));
        Assert.False(PackageStockService.DeductOnce(order, package));
        Assert.Equal(1, package.StockQuantity);
    }

    [Fact]
    public void RepeatedCancellationRestoresStockOnlyOnce()
    {
        var order = new Order { ShippingBoxStockDeducted = true };
        var package = new ShippingBox { StockQuantity = 1 };

        Assert.True(PackageStockService.RestoreOnce(order, package));
        Assert.False(PackageStockService.RestoreOnce(order, package));
        Assert.Equal(2, package.StockQuantity);
    }
}
