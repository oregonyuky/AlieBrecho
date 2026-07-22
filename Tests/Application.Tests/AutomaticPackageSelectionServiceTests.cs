using Application.Common.Services;
using Domain.Entities;
using Xunit;

namespace Application.Tests;

public class AutomaticPackageSelectionServiceTests
{
    private static readonly ShippingBox[] Packages =
    [
        Box("p1", 1), Box("p3", 3), Box("p5", 5), Box("p8", 8), Box("p12", 12)
    ];

    [Theory]
    [InlineData(1, "p1")]
    [InlineData(3, "p3")]
    [InlineData(4, "p5")]
    [InlineData(6, "p8")]
    public void SelectsSmallestCompatiblePackage(int points, string expectedId)
    {
        var selected = AutomaticPackageSelectionService.SelectSmallestCompatible(Packages, points, 1m);
        Assert.Equal(expectedId, selected?.Id);
    }

    [Fact]
    public void IgnoresPackageWithoutStock()
    {
        var packages = new[] { Box("small", 3, stock: 0), Box("next", 5) };
        var selected = AutomaticPackageSelectionService.SelectSmallestCompatible(packages, 3, 1m);
        Assert.Equal("next", selected?.Id);
    }

    [Fact]
    public void IgnoresInactivePackage()
    {
        var packages = new[] { Box("small", 3, active: false), Box("next", 5) };
        var selected = AutomaticPackageSelectionService.SelectSmallestCompatible(packages, 3, 1m);
        Assert.Equal("next", selected?.Id);
    }

    [Fact]
    public void ReturnsNullWhenNoPackageIsCompatible()
    {
        var selected = AutomaticPackageSelectionService.SelectSmallestCompatible(Packages, 20, 1m);
        Assert.Null(selected);
    }

    [Fact]
    public void RespectsMaximumWeight()
    {
        var packages = new[] { Box("light", 5, maxWeight: 1m), Box("strong", 8, maxWeight: 10m) };
        var selected = AutomaticPackageSelectionService.SelectSmallestCompatible(packages, 4, 2m);
        Assert.Equal("strong", selected?.Id);
    }

    private static ShippingBox Box(string id, int capacity, int stock = 1, bool active = true, decimal? maxWeight = null) => new()
    {
        Id = id,
        Name = id,
        PackageCategoryId = $"category-{capacity}",
        PackageCategory = new PackageCategory
        {
            Id = $"category-{capacity}",
            Name = $"Categoria {capacity}",
            CapacityPoints = capacity,
            IsActive = true
        },
        StockQuantity = stock,
        IsActive = active,
        MaxWeight = maxWeight,
        Width = capacity,
        Length = capacity,
        Height = capacity
    };
}
