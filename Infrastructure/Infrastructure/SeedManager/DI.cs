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
 

        return services;
    }


    public static IHost SeedSystemData(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var serviceProvider = scope.ServiceProvider;

        var context = serviceProvider.GetRequiredService<DataContext>();
        if (!context.Roles.Any()) //if empty, thats mean never been seeded before
        {
            var roleSeeder = serviceProvider.GetRequiredService<RoleSeeder>();
            roleSeeder.GenerateDataAsync().Wait();

            var userAdminSeeder = serviceProvider.GetRequiredService<UserAdminSeeder>();
            userAdminSeeder.GenerateDataAsync().Wait();

        }

        return host;
    }
     


    //>>> Demo Seed

    public static IServiceCollection RegisterDemoSeedManager(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<CategorySeeder>();
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
        return host;
    }
}

