using Application.Common.Services;
using MediatR;

namespace Application.Features.OrderManager.Queries;

public sealed class AutomaticShippingItem
{
    public string ProductId { get; init; } = string.Empty;
    public int Quantity { get; init; }
}

public sealed class CalculateAutomaticShippingRequest : IRequest<CalculateAutomaticShippingResult>
{
    public string? DestinationPostCode { get; init; }
    public List<AutomaticShippingItem> Items { get; init; } = [];
}

public sealed class CalculateAutomaticShippingResult
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public decimal ShippingCost { get; init; }
    public string? PackageId { get; init; }
    public string? PackageName { get; init; }
    public int OccupationPoints { get; init; }
    public int CapacityPoints { get; init; }
    public string? CarrierName { get; init; }
}

public sealed class CalculateAutomaticShippingHandler(
    IAutomaticPackageSelectionService packageSelectionService,
    IShippingCostService shippingCostService)
    : IRequestHandler<CalculateAutomaticShippingRequest, CalculateAutomaticShippingResult>
{
    public async Task<CalculateAutomaticShippingResult> Handle(
        CalculateAutomaticShippingRequest request,
        CancellationToken cancellationToken)
    {
        var selection = await packageSelectionService.SelectBestPackageAsync(
            request.Items.Select(x => new PackageSelectionItem(x.ProductId, x.Quantity)).ToList(),
            cancellationToken);
        if (selection.Package is null)
        {
            return new CalculateAutomaticShippingResult
            {
                Success = false,
                Message = "Nenhuma embalagem disponivel comporta os produtos deste pedido.",
                OccupationPoints = selection.TotalOccupationPoints
            };
        }

        var box = selection.Package;
        var shippingPackage = new Domain.Entities.ShippingBox
        {
            Id = box.Id,
            Width = box.Width,
            Length = box.Length,
            Height = box.Height,
            Weight = (box.Weight ?? 0m) + selection.TotalProductWeight,
            InsuranceValue = box.InsuranceValue
        };
        var quote = await shippingCostService.CalculateQuoteAsync(
            shippingPackage,
            request.DestinationPostCode,
            cancellationToken);

        return new CalculateAutomaticShippingResult
        {
            Success = true,
            ShippingCost = quote.Cost,
            CarrierName = quote.CarrierName,
            PackageId = box.Id,
            PackageName = box.Name,
            OccupationPoints = selection.TotalOccupationPoints,
            CapacityPoints = box.CapacityPoints
        };
    }
}
