using Application.Common.CQS.Commands;
using Application.Common.CQS.Queries;
using Application.Common.Repositories;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Infrastructure.DataAccessManager.EFCore.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Infrastructure.DataAccessManager.EFCore;



public static class DI
{
    public static IServiceCollection RegisterDataAccess(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        var databaseProvider = configuration["DatabaseProvider"];

        // Register Context
        switch (databaseProvider)
        {
            //case "MySql":
            //    services.AddDbContext<DataContext>(options =>
            //        options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 21)))
            //        .LogTo(Log.Information, LogLevel.Information)
            //        .EnableSensitiveDataLogging()
            //    );
            //    services.AddDbContext<CommandContext>(options =>
            //        options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 21)))
            //        .LogTo(Log.Information, LogLevel.Information)
            //        .EnableSensitiveDataLogging()
            //    );
            //    services.AddDbContext<QueryContext>(options =>
            //        options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 21)))
            //        .LogTo(Log.Information, LogLevel.Information)
            //        .EnableSensitiveDataLogging()
            //    );
            //    break;

            case "SqlServer":
            default:
                services.AddDbContext<DataContext>(options =>
                    options.UseSqlServer(connectionString)
                    .LogTo(Log.Information, LogLevel.Information)
                    .EnableSensitiveDataLogging()
                );
                services.AddDbContext<CommandContext>(options =>
                    options.UseSqlServer(connectionString)
                    .LogTo(Log.Information, LogLevel.Information)
                    .EnableSensitiveDataLogging()
                );
                services.AddDbContext<QueryContext>(options =>
                    options.UseSqlServer(connectionString)
                    .LogTo(Log.Information, LogLevel.Information)
                    .EnableSensitiveDataLogging()
                );
                break;
            case "PostgreSQL":
                services.AddDbContext<DataContext>(options =>
                    options.UseNpgsql(connectionString)
                    .LogTo(Log.Information, LogLevel.Information)
                    .EnableSensitiveDataLogging()
                );
                services.AddDbContext<CommandContext>(options =>
                    options.UseNpgsql(connectionString)
                    .LogTo(Log.Information, LogLevel.Information)
                    .EnableSensitiveDataLogging()
                );
                services.AddDbContext<QueryContext>(options =>
                    options.UseNpgsql(connectionString)
                    .LogTo(Log.Information, LogLevel.Information)
                    .EnableSensitiveDataLogging()
                );
                break;
            // case "Sqlite":
            //     services.AddDbContext<DataContext>(options =>
            //         options.UseSqlite(connectionString)
            //         .LogTo(Log.Information, LogLevel.Information)
            //         .EnableSensitiveDataLogging()
            //     );
            //     services.AddDbContext<CommandContext>(options =>
            //         options.UseSqlite(connectionString)
            //         .LogTo(Log.Information, LogLevel.Information)
            //         .EnableSensitiveDataLogging()
            //     );
            //     services.AddDbContext<QueryContext>(options =>
            //         options.UseSqlite(connectionString)
            //         .LogTo(Log.Information, LogLevel.Information)
            //         .EnableSensitiveDataLogging()
            //     );
            //     break;
        }


        services.AddScoped<ICommandContext, CommandContext>();
        services.AddScoped<IQueryContext, QueryContext>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(ICommandRepository<>), typeof(CommandRepository<>));


        return services;
    }

    public static IHost CreateDatabase(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var serviceProvider = scope.ServiceProvider;

        // Create database using DataContext
        var dataContext = serviceProvider.GetRequiredService<DataContext>();
        dataContext.Database.EnsureCreated(); // Ensure database is created (development only)
        EnsureCustomerTable(dataContext);

        return host;
    }

    private static void EnsureCustomerTable(DataContext dataContext)
    {
        if (!dataContext.Database.IsNpgsql())
        {
            return;
        }

        const string createTableSql = """
                                      CREATE TABLE IF NOT EXISTS "Customer" (
                                          "Id" character varying(50) NOT NULL,
                                          "Name" character varying(255),
                                          "Description" character varying(4000),
                                          "Cpf" character varying(50),
                                          "PhoneNumber" character varying(255),
                                          "EmailAddress" character varying(255),
                                          "Street" character varying(255),
                                          "Number" character varying(50),
                                          "Neighborhood" character varying(255),
                                          "Complement" character varying(255),
                                          "City" character varying(255),
                                          "State" character varying(255),
                                          "PostalCode" character varying(50),
                                          "Country" character varying(255),
                                          "Website" character varying(255),
                                          "Instagram" character varying(255),
                                          "TwitterX" character varying(255),
                                          "TikTok" character varying(255),
                                          "CustomerStatus" character varying(255),
                                          "CreatedAt" timestamp with time zone NOT NULL,
                                          "IsDeleted" boolean NOT NULL DEFAULT FALSE,
                                          "CreatedAtUtc" timestamp with time zone,
                                          "CreatedById" character varying(450),
                                          "UpdatedAtUtc" timestamp with time zone,
                                          "UpdatedById" character varying(450),
                                          CONSTRAINT "PK_Customer" PRIMARY KEY ("Id")
                                      );
                                      """;

        const string createIndexesSql = """
                                        CREATE INDEX IF NOT EXISTS "IX_Customer_Name" ON "Customer" ("Name");
                                        CREATE INDEX IF NOT EXISTS "IX_Customer_Cpf" ON "Customer" ("Cpf");
                                        CREATE INDEX IF NOT EXISTS "IX_Customer_EmailAddress" ON "Customer" ("EmailAddress");
                                        """;

        dataContext.Database.ExecuteSqlRaw(createTableSql);
        dataContext.Database.ExecuteSqlRaw(createIndexesSql);
    }
}

