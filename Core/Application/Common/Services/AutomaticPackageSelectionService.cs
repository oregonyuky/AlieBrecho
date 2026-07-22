using Application.Common.CQS.Queries;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Services;

public sealed class AutomaticPackageSelectionService(IQueryContext context)
    : IAutomaticPackageSelectionService
{
    public async Task<PackageSelectionResult> SelectBestPackageAsync(
        IReadOnlyCollection<PackageSelectionItem> items,
        CancellationToken cancellationToken = default)
    {
        var normalizedItems = items
            .Where(x => !string.IsNullOrWhiteSpace(x.ProductId))
            .GroupBy(x => x.ProductId)
            .Select(x => new PackageSelectionItem(x.Key, x.Sum(item => Math.Max(item.Quantity, 1))))
            .ToList();
        if (normalizedItems.Count == 0)
        {
            return new PackageSelectionResult(null, 0, 0m);
        }

        var ids = normalizedItems.Select(x => x.ProductId).ToList();
        var products = await context.Product
            .AsNoTracking()
            .Include(x => x.Category)
            .Where(x => ids.Contains(x.Id) && !x.IsDeleted)
            .ToListAsync(cancellationToken);
        if (products.Count != ids.Count)
        {
            throw new InvalidOperationException("Um ou mais produtos nao foram encontrados para selecionar a embalagem.");
        }

        var quantities = normalizedItems.ToDictionary(x => x.ProductId, x => x.Quantity);
        var points = products.Sum(product =>
            Math.Max(product.Category?.PackageOccupationPoints ?? 1, 1) * quantities[product.Id]);
        var productWeight = products.Sum(product =>
            Math.Max(product.UnitWeight ?? 0m, 0m) * quantities[product.Id]);

        var packages = await context.ShippingBox
            .AsNoTracking()
            .Include(x => x.PackageCategory)
            .Where(x => !x.IsDeleted && x.IsActive && x.StockQuantity > 0 &&
                        x.PackageCategory != null && !x.PackageCategory.IsDeleted &&
                        x.PackageCategory.IsActive && x.PackageCategory.CapacityPoints >= points)
            .ToListAsync(cancellationToken);

        var selected = SelectSmallestCompatible(packages, points, productWeight);
        return new PackageSelectionResult(selected, points, productWeight);
    }

    public static ShippingBox? SelectSmallestCompatible(
        IEnumerable<ShippingBox> packages,
        int totalOccupationPoints,
        decimal totalProductWeight)
    {
        return packages
            .Where(x => x.IsActive && x.StockQuantity > 0)
            .Where(x => x.PackageCategory != null && x.PackageCategory.IsActive &&
                        x.PackageCategory.CapacityPoints >= totalOccupationPoints)
            .Where(x => !x.MaxWeight.HasValue || x.MaxWeight.Value >= totalProductWeight)
            .OrderBy(x => x.PackageCategory!.CapacityPoints)
            .ThenBy(x => (x.Length ?? decimal.MaxValue) * (x.Width ?? decimal.MaxValue) * (x.Height ?? decimal.MaxValue))
            .FirstOrDefault();
    }
}
