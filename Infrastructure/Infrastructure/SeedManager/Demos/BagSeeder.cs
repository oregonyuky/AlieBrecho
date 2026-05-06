using Application.Common.Repositories;
using Domain.Entities;
using Domain.Enums;

namespace Infrastructure.SeedManager.Demos;

public class BagSeeder
{
    private readonly ICommandRepository<Bag> _bagRepository;
    private readonly ICommandRepository<Customer> _customerRepository;
    private readonly ICommandRepository<Product> _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public BagSeeder(
        ICommandRepository<Bag> bagRepository,
        ICommandRepository<Customer> customerRepository,
        ICommandRepository<Product> productRepository,
        IUnitOfWork unitOfWork)
    {
        _bagRepository = bagRepository;
        _customerRepository = customerRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task GenerateDataAsync()
    {
        var customers = _customerRepository.GetQuery().ToList();
        var products = _productRepository.GetQuery().ToList();

        if (!customers.Any() || !products.Any())
        {
            return;
        }

        var customer1 = customers.First();
        var customer2 = customers.Count > 1 ? customers[1] : customers.First();

        var product1 = products.First();
        var product2 = products.Count > 1 ? products[1] : products.First();

        var bag1Items = new List<BagItem>
        {
            new()
            {
                ProductId = product1.Id,
                ProductName = product1.Name,
                Quantity = 1,
                Price = product1.UnitPrice ?? 0m,
                Weight = product1.UnitWeight ?? 0m,
                IsPaid = true,
                IsReserved = false,
                AddedAt = DateTime.UtcNow.AddHours(-3),
                PaidAt = DateTime.UtcNow.AddHours(-1)
            },
            new()
            {
                ProductId = product2.Id,
                ProductName = product2.Name,
                Quantity = 1,
                Price = product2.UnitPrice ?? 0m,
                Weight = product2.UnitWeight ?? 0m,
                IsPaid = false,
                IsReserved = true,
                ReservationExpiresAt = DateTime.UtcNow.AddHours(12),
                AddedAt = DateTime.UtcNow.AddHours(-2)
            }
        };

        var bag1 = new Bag
        {
            CustomerId = customer1.Id,
            Status = BagStatus.Active,
            CreatedAt = DateTime.UtcNow.AddHours(-4),
            LastInteractionAt = DateTime.UtcNow.AddMinutes(-30),
            ExpirationDate = DateTime.UtcNow.AddDays(2),
            ShippingCost = 19.90m,
            TotalItemsValue = bag1Items.Sum(x => x.Price * x.Quantity),
            TotalWeight = bag1Items.Sum(x => x.Weight * x.Quantity),
            AllItemsPaid = bag1Items.All(x => x.IsPaid),
            Notes = "Sacola ativa de demonstração.",
            Items = bag1Items
        };

        var bag2Items = new List<BagItem>
        {
            new()
            {
                ProductId = product2.Id,
                ProductName = product2.Name,
                Quantity = 1,
                Price = product2.UnitPrice ?? 0m,
                Weight = product2.UnitWeight ?? 0m,
                IsPaid = true,
                IsReserved = false,
                AddedAt = DateTime.UtcNow.AddDays(-2),
                PaidAt = DateTime.UtcNow.AddDays(-2).AddHours(1)
            }
        };

        var bag2 = new Bag
        {
            CustomerId = customer2.Id,
            Status = BagStatus.Closed,
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            LastInteractionAt = DateTime.UtcNow.AddDays(-1),
            ClosedAt = DateTime.UtcNow.AddDays(-1),
            ExpirationDate = DateTime.UtcNow.AddDays(1),
            ShippingCost = 0m,
            TotalItemsValue = bag2Items.Sum(x => x.Price * x.Quantity),
            TotalWeight = bag2Items.Sum(x => x.Weight * x.Quantity),
            AllItemsPaid = true,
            Notes = "Sacola fechada de demonstração.",
            Items = bag2Items
        };

        await _bagRepository.CreateAsync(bag1);
        await _bagRepository.CreateAsync(bag2);

        await _unitOfWork.SaveAsync();
    }
}
