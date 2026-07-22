using Application.Common;
using Application.Common.Repositories;
using Application.Common.Services.MercadoPagoManager;
using ASPNET.BackEnd.Controllers;
using ASPNET.BackEnd.Hubs;
using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Infrastructure.DataAccessManager.EFCore.Repositories;
using Infrastructure.MercadoPagoManager;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Application.Tests;

public class ReservationConcurrencyTests
{
    [Fact]
    public async Task Concurrent_checkout_for_same_product_allows_only_one_reservation()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"aliebrecho-reservation-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={databasePath};Cache=Shared;Default Timeout=10";

        try
        {
            await using (var setup = CreateContext(connectionString))
            {
                await setup.Database.EnsureCreatedAsync();
                await setup.Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT INTO "Product" ("Id", "Name", "ProductAvailable", "RowVersion", "IsDeleted")
                    VALUES ({"product-1"}, {"Peca unica"}, {true}, {new byte[] { 1 }}, {false});
                    """);
            }

            var request1 = CreateRequest("customer-1");
            var request2 = CreateRequest("customer-2");
            await using var context1 = CreateContext(connectionString);
            await using var context2 = CreateContext(connectionString);
            var controller1 = CreateController(context1, "payment-1");
            var controller2 = CreateController(context2, "payment-2");

            var results = await Task.WhenAll(
                controller1.CheckoutBagAsync(request1, CancellationToken.None),
                controller2.CheckoutBagAsync(request2, CancellationToken.None));

            var statusCodes = results
                .Select(x => Assert.IsAssignableFrom<ObjectResult>(x.Result).StatusCode)
                .OrderBy(x => x)
                .ToArray();
            Assert.Equal([StatusCodes.Status200OK, StatusCodes.Status409Conflict], statusCodes);

            await using var verification = CreateContext(connectionString);
            Assert.Equal(1, await verification.BagItem.CountAsync(x => x.ProductId == "product-1" && x.IsReserved));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }
        }
    }

    [Fact]
    public async Task Concurrent_payment_confirmations_for_same_product_accept_only_one_bag()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"aliebrecho-payment-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={databasePath};Cache=Shared;Default Timeout=10";

        try
        {
            await using (var setup = CreateContext(connectionString))
            {
                await setup.Database.EnsureCreatedAsync();
                await setup.Database.ExecuteSqlRawAsync("DROP INDEX \"UX_BagItem_ActiveReservation_ProductId\";");
                await setup.Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT INTO "Product" ("Id", "Name", "ProductAvailable", "RowVersion", "IsDeleted")
                    VALUES ({"product-1"}, {"Peca unica"}, {true}, {new byte[] { 1 }}, {false});
                    """);

                setup.Bag.AddRange(CreateReservedBag("bag-1", "customer-1"), CreateReservedBag("bag-2", "customer-2"));
                await setup.SaveChangesAsync();
            }

            await using var context1 = CreateContext(connectionString);
            await using var context2 = CreateContext(connectionString);
            var controller1 = CreatePixController(context1, new FakeMercadoPagoService("payment-1", "bag-1"));
            var controller2 = CreatePixController(context2, new FakeMercadoPagoService("payment-2", "bag-2"));

            var outcomes = await Task.WhenAll(
                ConfirmPaymentAsync(controller1, "payment-1"),
                ConfirmPaymentAsync(controller2, "payment-2"));

            Assert.Equal(1, outcomes.Count(x => x == StatusCodes.Status200OK));
            Assert.Equal(1, outcomes.Count(x => x == StatusCodes.Status409Conflict));

            await using var verification = CreateContext(connectionString);
            Assert.Equal(1, await verification.BagItem.CountAsync(x => x.ProductId == "product-1" && x.IsPaid));
            Assert.False((await verification.Product.SingleAsync(x => x.Id == "product-1")).ProductAvailable);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }
        }
    }

    [Fact]
    public void Model_has_row_version_and_unique_active_reservation_index()
    {
        using var context = CreateContext("Data Source=:memory:");
        var productRowVersion = context.Model.FindEntityType(typeof(Product))!.FindProperty(nameof(Product.RowVersion))!;
        Assert.True(productRowVersion.IsConcurrencyToken);

        var index = context.Model.FindEntityType(typeof(BagItem))!.GetIndexes()
            .Single(x => x.GetDatabaseName() == "UX_BagItem_ActiveReservation_ProductId");
        Assert.True(index.IsUnique);
        Assert.Equal("[ProductId] IS NOT NULL AND [IsDeleted] = 0 AND [IsReserved] = 1", index.GetFilter());
    }

    [Fact]
    public async Task Migration_applies_cleanly_to_existing_sqlite_schema()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<DataContext>().UseSqlite(connection).Options;
        await using var context = new DataContext(options);

        await context.Database.ExecuteSqlRawAsync("""
            CREATE TABLE "Product" ("Id" TEXT NOT NULL PRIMARY KEY, "Name" TEXT NOT NULL);
            CREATE TABLE "BagItem" (
                "Id" TEXT NOT NULL PRIMARY KEY,
                "ProductId" TEXT NULL,
                "IsDeleted" INTEGER NOT NULL,
                "IsReserved" INTEGER NOT NULL,
                "IsPaid" INTEGER NOT NULL,
                "ReservationExpiresAt" TEXT NULL,
                "AddedAt" TEXT NOT NULL
            );
            CREATE TABLE "__EFMigrationsHistory" (
                "MigrationId" TEXT NOT NULL PRIMARY KEY,
                "ProductVersion" TEXT NOT NULL
            );
            INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
            VALUES ('20260718120000_AddOrderDetailProductSnapshot', '10.0.0');
            """);

        await context.Database.MigrateAsync();

        var columns = await context.Database.SqlQueryRaw<string>(
            "SELECT name AS Value FROM pragma_table_info('Product')").ToListAsync();
        Assert.Contains("RowVersion", columns);
        var indexes = await context.Database.SqlQueryRaw<string>(
            "SELECT name AS Value FROM pragma_index_list('BagItem')").ToListAsync();
        Assert.Contains("UX_BagItem_ActiveReservation_ProductId", indexes);
    }

    private static CommandContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseSqlite(connectionString)
            .Options;
        return new CommandContext(options);
    }

    private static CheckoutBagRequest CreateRequest(string customerId) => new()
    {
        CustomerId = customerId,
        PayerCpf = "12345678901",
        ShippingDetail = new CheckoutBagShippingDetailRequest
        {
            FirstName = "Cliente",
            Email = $"{customerId}@example.com"
        },
        Items = [new CheckoutBagItemRequest { ProductId = "product-1", Quantity = 1 }]
    };

    private static BagController CreateController(CommandContext context, string paymentId)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging();
        serviceCollection.AddSignalR();
        var services = serviceCollection.BuildServiceProvider();
        return new BagController(
            null!,
            new CommandRepository<Bag>(context),
            new CommandRepository<BagItem>(context),
            new CommandRepository<Product>(context),
            new CommandRepository<BagSettings>(context),
            new CommandRepository<BagExpirationHistory>(context),
            new UnitOfWork(context),
            new FakeMercadoPagoService(paymentId, null),
            Options.Create(new MercadoPagoSettings { ExpirationMinutes = 30 }),
            services.GetRequiredService<IHubContext<OrderNotificationsHub>>(),
            services.GetRequiredService<IHubContext<CatalogNotificationsHub>>());
    }

    private static PixController CreatePixController(CommandContext context, IMercadoPagoService mercadoPagoService)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging();
        serviceCollection.AddSignalR();
        var services = serviceCollection.BuildServiceProvider();
        return new PixController(
            new CommandRepository<Order>(context),
            new CommandRepository<Bag>(context),
            new CommandRepository<Payment>(context),
            new CommandRepository<Product>(context),
            new CommandRepository<ShippingBox>(context),
            new UnitOfWork(context),
            mercadoPagoService,
            Options.Create(new MercadoPagoSettings()),
            services.GetRequiredService<IHubContext<OrderNotificationsHub>>(),
            services.GetRequiredService<IHubContext<CatalogNotificationsHub>>());
    }

    private static Bag CreateReservedBag(string bagId, string customerId) => new()
    {
        Id = bagId,
        CustomerId = customerId,
        ExpirationDate = DateTime.UtcNow.AddMinutes(30),
        TotalItemsValue = 10m,
        Items =
        [
            new BagItem
            {
                BagId = bagId,
                ProductId = "product-1",
                ProductName = "Peca unica",
                Price = 10m,
                Weight = 1m,
                IsReserved = true,
                ReservationExpiresAt = DateTime.UtcNow.AddMinutes(30)
            }
        ]
    };

    private static async Task<int> ConfirmPaymentAsync(PixController controller, string paymentId)
    {
        try
        {
            var result = await controller.GetStatusAsync(paymentId, CancellationToken.None);
            return Assert.IsAssignableFrom<ObjectResult>(result.Result).StatusCode ?? StatusCodes.Status200OK;
        }
        catch (ProductUnavailableException)
        {
            return StatusCodes.Status409Conflict;
        }
    }

    private sealed class FakeMercadoPagoService(string paymentId, string? bagId) : IMercadoPagoService
    {
        public Task<MercadoPagoPixPaymentResult> CreatePixPaymentAsync(
            MercadoPagoPixPaymentRequest request,
            CancellationToken cancellationToken) => Task.FromResult(
                new MercadoPagoPixPaymentResult(paymentId, "pending", "qr-base64", "qr", request.DateOfExpiration, request.TransactionAmount));

        public Task<MercadoPagoPaymentStatusResult> GetPaymentAsync(string id, CancellationToken cancellationToken) =>
            Task.FromResult(new MercadoPagoPaymentStatusResult(
                paymentId, "approved", null, bagId, 10m, DateTime.UtcNow, DateTime.UtcNow.AddMinutes(30)));
    }
}
