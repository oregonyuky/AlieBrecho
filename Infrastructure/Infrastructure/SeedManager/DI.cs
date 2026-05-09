using Infrastructure.DataAccessManager.EFCore.Contexts;
using Infrastructure.SeedManager.Demos;
using Infrastructure.SeedManager.Systems;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Infrastructure.SeedManager;

public static class DI
{
    //>>> System Seed

    public static IServiceCollection RegisterSystemSeedManager(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<RoleSeeder>();
        services.AddScoped<UserAdminSeeder>();
        services.AddScoped<CompanySeeder>();

        return services;
    }


    public static IHost SeedSystemData(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var serviceProvider = scope.ServiceProvider;

        var context = serviceProvider.GetRequiredService<DataContext>();

        var roleSeeder = serviceProvider.GetRequiredService<RoleSeeder>();
        roleSeeder.GenerateDataAsync().Wait();

        var userAdminSeeder = serviceProvider.GetRequiredService<UserAdminSeeder>();
        userAdminSeeder.GenerateDataAsync().Wait();

        if (!context.Company.Any())
        {
            var companySeeder = serviceProvider.GetRequiredService<CompanySeeder>();
            companySeeder.GenerateDataAsync().Wait();

        }

        return host;
    }
     


    //>>> Demo Seed

    public static IServiceCollection RegisterDemoSeedManager(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<CategorySeeder>();
        services.AddScoped<ProductSeeder>();
        services.AddScoped<CustomerSeeder>();
        services.AddScoped<PaymentTypeSeeder>();
        services.AddScoped<OrderSeeder>();
        services.AddScoped<BagSeeder>();
        services.AddScoped<ShippingBoxSeeder>();
        return services;
    }
    public static IHost SeedDemoData(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var serviceProvider = scope.ServiceProvider;

        var context = serviceProvider.GetRequiredService<DataContext>();
        if (!context.Category.Any()) //if empty, thats mean never been seeded before
        {
            var categorySeeder = serviceProvider.GetRequiredService<CategorySeeder>();
            categorySeeder.GenerateDataAsync().Wait();

        }

        if (!context.Product.Any())
        {
            var productSeeder = serviceProvider.GetRequiredService<ProductSeeder>();
            productSeeder.GenerateDataAsync().Wait();
        }

        if (!context.Customer.Any()) //if empty, thats mean never been seeded before
        {
            var customerSeeder = serviceProvider.GetRequiredService<CustomerSeeder>();
            customerSeeder.GenerateDataAsync().Wait();
        }

        if (!context.PaymentType.Any())
        {
            var paymentTypeSeeder = serviceProvider.GetRequiredService<PaymentTypeSeeder>();
            paymentTypeSeeder.GenerateDataAsync().Wait();
        }

        if (!context.Order.Any())
        {
            var orderSeeder = serviceProvider.GetRequiredService<OrderSeeder>();
            orderSeeder.GenerateDataAsync().Wait();
        }

        if (!context.Bag.Any() || !context.BagItem.Any())
        {
            var bagSeeder = serviceProvider.GetRequiredService<BagSeeder>();
            bagSeeder.GenerateDataAsync().Wait();
        }
        if(!context.ShippingBox.Any())
        {
            var shippingBoxSeeder = serviceProvider.GetRequiredService<ShippingBoxSeeder>();
            shippingBoxSeeder.GenerateDataAsync().Wait();
        }
        return host;
    }
}
