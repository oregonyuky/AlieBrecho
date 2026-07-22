using Application.Common.Repositories;
using Application.Common.Services;
using Application.Features.OrderManager.Commands;
using Application.Features.OrderManager.Queries;
using AutoMapper;
using Domain.Common;
using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Application.Tests;

public class OrderSnapshotTests
{
    [Fact]
    public async Task CreateOrderStoresProductSnapshot()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<DataContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new QueryContext(options);
        await context.Database.EnsureCreatedAsync();

        var customer = new Customer { Name = "Cliente", PostalCode = "01001000" };
        var product = new Product
        {
            Name = "Vestido azul",
            MainImageURL = "vestido-azul.jpg",
            Picture1 = "fallback.jpg",
            UnitPrice = 120m
        };

        context.Customer.Add(customer);
        context.Product.Add(product);
        context.ShippingBox.Add(new ShippingBox
        {
            Name = "Caixa teste",
            CapacityPoints = 5,
            StockQuantity = 1,
            IsActive = true,
            Width = 20,
            Length = 20,
            Height = 10,
            Weight = 0.2m
        });
        await context.SaveChangesAsync();

        var orderRepository = new CapturingRepository<Order>();
        var handler = new CreateOrderHandler(
            orderRepository,
            new CapturingRepository<Product>(),
            new NoOpUnitOfWork(),
            context,
            new FixedShippingCostService(),
            new AutomaticPackageSelectionService(context));

        await handler.Handle(new CreateOrderRequest
        {
            CustomerId = customer.Id,
            OrderDetails =
            [
                new OrderDetailEditDto
                {
                    ProductId = product.Id,
                    Quantity = 2
                }
            ]
        }, CancellationToken.None);

        product.Name = "Vestido editado";
        product.MainImageURL = "vestido-editado.jpg";

        var item = Assert.Single(orderRepository.CreatedEntities).OrderDetails.Single();
        Assert.Equal("Vestido azul", item.ProductName);
        Assert.Equal("vestido-azul.jpg", item.ProductImageUrl);
        Assert.Equal(2, item.Quantity);
        Assert.Equal(120m, item.UnitPrice);
        Assert.Equal(240m, item.TotalPrice);
    }

    [Fact]
    public void GetOrderListMapsItemsFromSnapshotWithLegacyFallback()
    {
        var mapper = new MapperConfiguration(config => config.AddProfile<GetOrderListProfile>())
            .CreateMapper();

        var snapshotOrder = new Order
        {
            OrderDetails =
            [
                new OrderDetail
                {
                    ProductId = "snapshot-product",
                    ProductName = "Nome congelado",
                    ProductImageUrl = "snapshot.jpg",
                    Product = new Product { Name = "Nome editado", MainImageURL = "live.jpg" },
                    Quantity = 1,
                    UnitPrice = 50m,
                    TotalPrice = 50m
                }
            ]
        };

        var legacyOrder = new Order
        {
            OrderDetails =
            [
                new OrderDetail
                {
                    ProductId = "legacy-product",
                    Product = new Product { Name = "Nome legado", MainImageURL = null, Picture1 = "legacy.jpg" },
                    Quantity = 3,
                    UnitPrice = 20m,
                    TotalPrice = 60m
                }
            ]
        };

        var snapshotDto = mapper.Map<GetOrderListDto>(snapshotOrder);
        var legacyDto = mapper.Map<GetOrderListDto>(legacyOrder);

        var snapshotItem = Assert.Single(snapshotDto.Items!);
        Assert.Equal("Nome congelado", snapshotItem.ProductName);
        Assert.Equal("snapshot.jpg", snapshotItem.ProductImageUrl);
        Assert.Equal(50m, snapshotItem.TotalPrice);

        var legacyItem = Assert.Single(legacyDto.Items!);
        Assert.Equal("Nome legado", legacyItem.ProductName);
        Assert.Equal("legacy.jpg", legacyItem.ProductImageUrl);
        Assert.Equal(3, legacyItem.Quantity);
    }

    private sealed class CapturingRepository<T> : ICommandRepository<T>
        where T : BaseEntity
    {
        public List<T> CreatedEntities { get; } = [];

        public Task CreateAsync(T entity, CancellationToken cancellationToken = default)
        {
            CreatedEntities.Add(entity);
            return Task.CompletedTask;
        }

        public void Create(T entity) => CreatedEntities.Add(entity);
        public void Update(T entity) { }
        public void Delete(T entity) { }
        public void Purge(T entity) { }
        public Task<T?> GetAsync(string id, CancellationToken cancellationToken = default) => Task.FromResult<T?>(null);
        public T? Get(string id) => null;
        public IQueryable<T> GetQuery() => Enumerable.Empty<T>().AsQueryable();
    }

    private sealed class NoOpUnitOfWork : IUnitOfWork
    {
        public Task SaveAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default) => operation();
        public Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation, System.Data.IsolationLevel isolationLevel, CancellationToken cancellationToken = default) => operation();
        public void Save() { }
    }

    private sealed class FixedShippingCostService : IShippingCostService
    {
        public Task<decimal> CalculateAsync(
            ShippingBox? shippingBox,
            string? destinationPostCode,
            CancellationToken cancellationToken = default) => Task.FromResult(0m);
    }
}
