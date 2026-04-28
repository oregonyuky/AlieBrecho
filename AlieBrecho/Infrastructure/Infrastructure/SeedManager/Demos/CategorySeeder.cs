using Application.Common.Repositories;
using Domain.Entities;

namespace Infrastructure.SeedManager.Demos;

public class CategorySeeder
{
    private readonly ICommandRepository<Category> _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CategorySeeder(
        ICommandRepository<Category> categoryRepository,
        IUnitOfWork unitOfWork
    )
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task GenerateDataAsync()
    {
        var categories = new List<Category>
        {
            new Category { Name = "Clothing", Description = "Apparel and fashion items" },
            new Category { Name = "Electronics", Description = "Devices and gadgets" },
            new Category { Name = "Home", Description = "Home and kitchen items" },
            new Category { Name = "Beauty", Description = "Beauty and personal care" },
            new Category { Name = "Sports", Description = "Sports and outdoor products" }
        };

        foreach (var category in categories)
        {
            await _categoryRepository.CreateAsync(category);
        }

        await _unitOfWork.SaveAsync();
    }
}