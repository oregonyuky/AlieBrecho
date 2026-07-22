using Application.Common.Services;
using Application.Common.Repositories;
using Application.Features.PackageCategoryManager.Commands;
using Domain.Common;
using Domain.Entities;
using Xunit;

namespace Application.Tests;

public sealed class PackageCategoryTests
{
    [Fact]
    public void AcceptsValidCapacity()
    {
        var result = new CreatePackageCategoryValidator().Validate(new CreatePackageCategoryRequest
        {
            Name = "Caixa M",
            CapacityPoints = 8
        });
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-5)]
    public void RejectsNonPositiveCapacity(int capacity)
    {
        var result = new CreatePackageCategoryValidator().Validate(new CreatePackageCategoryRequest
        {
            Name = "Caixa inválida",
            CapacityPoints = capacity
        });
        Assert.False(result.IsValid);
    }

    [Fact]
    public void AcceptsCapacityEdit()
    {
        var result = new UpdatePackageCategoryValidator().Validate(new UpdatePackageCategoryRequest
        {
            Id = "category-id",
            Name = "Caixa G",
            CapacityPoints = 12
        });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ShippingBoxUsesCategoryCapacity()
    {
        var category = new PackageCategory { Id = "medium", Name = "Caixa M", CapacityPoints = 8, IsActive = true };
        var box = new ShippingBox
        {
            Id = "physical-box", Name = "Papelão 40x30x15", PackageCategoryId = category.Id,
            PackageCategory = category, StockQuantity = 1, IsActive = true
        };
        Assert.Equal(8, box.PackageCategory.CapacityPoints);
    }

    [Fact]
    public void AutomaticSelectionIgnoresInactiveCategory()
    {
        var inactive = Box("inactive", 3, false);
        var active = Box("active", 5, true);
        var selected = AutomaticPackageSelectionService.SelectSmallestCompatible([inactive, active], 3, 0m);
        Assert.Equal("active", selected?.Id);
    }

    [Fact]
    public async Task CreatesEditsAndDeactivatesCategory()
    {
        var repository = new MemoryRepository<PackageCategory>();
        var unitOfWork = new NoOpUnitOfWork();
        var created = (await new CreatePackageCategoryHandler(repository, unitOfWork).Handle(
            new CreatePackageCategoryRequest { Name = "Caixa M", CapacityPoints = 8 }, CancellationToken.None)).Data!;
        Assert.Equal(8, created.CapacityPoints);

        var updated = (await new UpdatePackageCategoryHandler(repository, unitOfWork).Handle(
            new UpdatePackageCategoryRequest { Id = created.Id, Name = "Caixa M", CapacityPoints = 9 }, CancellationToken.None)).Data!;
        Assert.Equal(9, updated.CapacityPoints);

        var deactivated = (await new DeactivatePackageCategoryHandler(repository, unitOfWork).Handle(
            new DeactivatePackageCategoryRequest { Id = created.Id }, CancellationToken.None)).Data!;
        Assert.False(deactivated.IsActive);
    }

    private static ShippingBox Box(string id, int capacity, bool categoryActive) => new()
    {
        Id = id, Name = id, IsActive = true, StockQuantity = 1, Width = 1, Length = 1, Height = 1,
        PackageCategoryId = $"category-{id}",
        PackageCategory = new PackageCategory
        {
            Id = $"category-{id}", Name = id, CapacityPoints = capacity, IsActive = categoryActive
        }
    };

    private sealed class MemoryRepository<T> : ICommandRepository<T> where T : BaseEntity
    {
        private readonly List<T> items = [];
        public Task CreateAsync(T entity, CancellationToken cancellationToken = default) { items.Add(entity); return Task.CompletedTask; }
        public void Create(T entity) => items.Add(entity);
        public void Update(T entity) { }
        public void Delete(T entity) => entity.IsDeleted = true;
        public void Purge(T entity) => items.Remove(entity);
        public Task<T?> GetAsync(string id, CancellationToken cancellationToken = default) => Task.FromResult(items.SingleOrDefault(x => x.Id == id));
        public T? Get(string id) => items.SingleOrDefault(x => x.Id == id);
        public IQueryable<T> GetQuery() => items.AsQueryable();
    }

    private sealed class NoOpUnitOfWork : IUnitOfWork
    {
        public void Save() { }
        public Task SaveAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default) => operation();
        public Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation, System.Data.IsolationLevel isolationLevel, CancellationToken cancellationToken = default) => operation();
    }
}
