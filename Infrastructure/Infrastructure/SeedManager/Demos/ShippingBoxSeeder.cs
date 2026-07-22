using Application.Common.Repositories;
using Domain.Entities;

namespace Infrastructure.SeedManager.Demos;

public class ShippingBoxSeeder
{
    private readonly ICommandRepository<ShippingBox> _shippingBoxRepository;
    private readonly ICommandRepository<PackageCategory> _packageCategoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ShippingBoxSeeder(
        ICommandRepository<ShippingBox> shippingBoxRepository,
        ICommandRepository<PackageCategory> packageCategoryRepository,
        IUnitOfWork unitOfWork
    )
    {
        _shippingBoxRepository = shippingBoxRepository;
        _packageCategoryRepository = packageCategoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task GenerateDataAsync()
    {
        var categories = _packageCategoryRepository.GetQuery().ToDictionary(x => x.CapacityPoints);
        var boxes = new List<ShippingBox>
        {
            Box("Envelope P", 10, 15, 5, 0.3m, 50, categories[1]),
            Box("Envelope M", 20, 25, 10, 0.8m, 100, categories[3]),
            Box("Caixa P", 30, 35, 15, 1.5m, 150, categories[5]),
            Box("Caixa M", 40, 45, 20, 2.5m, 200, categories[8]),
            Box("Caixa G", 50, 60, 30, 4.0m, 300, categories[12])
        };

        foreach (var box in boxes)
        {
            await _shippingBoxRepository.CreateAsync(box);
        }

        await _unitOfWork.SaveAsync();
    }

    private static ShippingBox Box(string name, decimal width, decimal length, decimal height, decimal weight, decimal insurance, PackageCategory category) => new()
    {
        Name = name, Width = width, Length = length, Height = height, Weight = weight,
        InsuranceValue = insurance, PackageCategoryId = category.Id, StockQuantity = 1, IsActive = true
    };
}
