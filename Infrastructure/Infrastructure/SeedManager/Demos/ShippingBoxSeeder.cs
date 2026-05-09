using Application.Common.Repositories;
using Domain.Entities;

namespace Infrastructure.SeedManager.Demos;

public class ShippingBoxSeeder
{
    private readonly ICommandRepository<ShippingBox> _shippingBoxRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ShippingBoxSeeder(
        ICommandRepository<ShippingBox> shippingBoxRepository,
        IUnitOfWork unitOfWork
    )
    {
        _shippingBoxRepository = shippingBoxRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task GenerateDataAsync()
    {
        var boxes = new List<ShippingBox>
        {
            new ShippingBox { Width = 10, Length = 15, Height = 5, Weight = 0.3m, InsuranceValue = 50 },
            new ShippingBox { Width = 20, Length = 25, Height = 10, Weight = 0.8m, InsuranceValue = 100 },
            new ShippingBox { Width = 30, Length = 35, Height = 15, Weight = 1.5m, InsuranceValue = 150 },
            new ShippingBox { Width = 40, Length = 45, Height = 20, Weight = 2.5m, InsuranceValue = 200 },
            new ShippingBox { Width = 50, Length = 60, Height = 30, Weight = 4.0m, InsuranceValue = 300 }
        };

        foreach (var box in boxes)
        {
            await _shippingBoxRepository.CreateAsync(box);
        }

        await _unitOfWork.SaveAsync();
    }
}