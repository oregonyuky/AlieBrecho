using Application.Common.Repositories;
using Domain.Entities;

namespace Infrastructure.SeedManager.Demos;

public class ProductSeeder
{
    private readonly ICommandRepository<Product> _productRepository;
    private readonly ICommandRepository<Category> _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ProductSeeder(
        ICommandRepository<Product> productRepository,
        ICommandRepository<Category> categoryRepository,
        IUnitOfWork unitOfWork
    )
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task GenerateDataAsync()
    {
        var categories = _categoryRepository.GetQuery().ToList();
        var clothingCategoryId = categories.FirstOrDefault(x => x.Name == "Clothing")?.Id;
        var beautyCategoryId = categories.FirstOrDefault(x => x.Name == "Beauty")?.Id;
        var sportsCategoryId = categories.FirstOrDefault(x => x.Name == "Sports")?.Id;

        var products = new List<Product>
        {
            CreateProduct(
                "Vintage Denim Jacket",
                clothingCategoryId,
                129.90m,
                169.90m,
                "Jaqueta jeans vintage em bom estado.",
                "Jaqueta jeans vintage com lavagem clara, bolsos frontais e modelagem casual.",
                [
                    new ProductSize { Size = "P", Bust = 92, Sleeve = 58, Length = 58 },
                    new ProductSize { Size = "M", Bust = 98, Sleeve = 60, Length = 60 }
                ]
            ),
            CreateProduct(
                "Floral Summer Dress",
                clothingCategoryId,
                89.90m,
                119.90m,
                "Vestido floral leve.",
                "Vestido floral de tecido leve, ideal para dias quentes e looks casuais.",
                [
                    new ProductSize { Size = "P", Bust = 86, Sleeve = 0, Length = 88 },
                    new ProductSize { Size = "M", Bust = 92, Sleeve = 0, Length = 90 },
                    new ProductSize { Size = "G", Bust = 98, Sleeve = 0, Length = 92 }
                ]
            ),
            CreateProduct(
                "Cotton Basic Shirt",
                clothingCategoryId,
                39.90m,
                59.90m,
                "Camisa basica de algodao.",
                "Camisa de algodao com corte confortavel para uso diario.",
                [
                    new ProductSize { Size = "M", Bust = 96, Sleeve = 21, Length = 68 },
                    new ProductSize { Size = "G", Bust = 104, Sleeve = 22, Length = 71 }
                ]
            ),
            CreateProduct(
                "Retro Sports Hoodie",
                sportsCategoryId,
                99.90m,
                139.90m,
                "Moletom esportivo retro.",
                "Moletom com capuz, acabamento macio e visual esportivo retro.",
                [
                    new ProductSize { Size = "M", Bust = 104, Sleeve = 62, Length = 66 },
                    new ProductSize { Size = "G", Bust = 112, Sleeve = 64, Length = 69 }
                ]
            ),
            CreateProduct(
                "Beauty Organizer Bag",
                beautyCategoryId,
                49.90m,
                69.90m,
                "Necessaire organizadora.",
                "Necessaire com compartimentos internos para maquiagem e itens pessoais.",
                []
            )
        };

        foreach (var product in products)
        {
            await _productRepository.CreateAsync(product);
        }

        await _unitOfWork.SaveAsync();
    }

    private static Product CreateProduct(
        string name,
        string? categoryId,
        decimal unitPrice,
        decimal oldPrice,
        string shortDescription,
        string longDescription,
        List<ProductSize> sizes
        )
    {
        var product = new Product
        {
            Name = name,
            CategoryID = categoryId,
            UnitPrice = unitPrice,
            OldPrice = oldPrice,
            UnitWeight = 0.5m,
            DiscountPercent = oldPrice > 0 ? Math.Round((oldPrice - unitPrice) / oldPrice * 100, 2) : null,
            ProductAvailable = true,
            AddBadge = true,
            AltText = name,
            ShortDescription = shortDescription,
            LongDescription = longDescription,
            Note = "Demo product",
            Sizes = sizes
        };

        foreach (var size in product.Sizes)
        {
            size.ProductId = product.Id;
        }

        return product;
    }
}
