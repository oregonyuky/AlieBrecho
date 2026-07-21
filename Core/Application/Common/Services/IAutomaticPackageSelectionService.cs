using Domain.Entities;

namespace Application.Common.Services;

public interface IAutomaticPackageSelectionService
{
    Task<PackageSelectionResult> SelectBestPackageAsync(
        IReadOnlyCollection<PackageSelectionItem> items,
        CancellationToken cancellationToken = default);
}

public sealed record PackageSelectionItem(string ProductId, int Quantity);

public sealed record PackageSelectionResult(
    ShippingBox? Package,
    int TotalOccupationPoints,
    decimal TotalProductWeight)
{
    public bool IsSuccess => Package is not null;
}
