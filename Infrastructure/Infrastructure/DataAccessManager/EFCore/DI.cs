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
        EnsureOrderMelhorEnvioCartColumns(dataContext);
        EnsurePaidOrderProductsUnavailable(dataContext);
        EnsurePaidBagProductsUnavailable(dataContext);
        EnsureDropConfigTable(dataContext);

        return host;
    }

    private static void EnsureDropConfigTable(DataContext dataContext)
    {
        if (dataContext.Database.IsSqlServer())
        {
            dataContext.Database.ExecuteSqlRaw("""
                                               IF OBJECT_ID('dbo.DropConfig', 'U') IS NULL
                                               BEGIN
                                                   CREATE TABLE [DropConfig] (
                                                       [Id] nvarchar(50) NOT NULL,
                                                       [IsDeleted] bit NOT NULL CONSTRAINT [DF_DropConfig_IsDeleted] DEFAULT CAST(0 AS bit),
                                                       [CreatedAtUtc] datetime2 NULL,
                                                       [CreatedById] nvarchar(450) NULL,
                                                       [UpdatedAtUtc] datetime2 NULL,
                                                       [UpdatedById] nvarchar(450) NULL,
                                                       [Titulo] nvarchar(255) NOT NULL,
                                                       [Subtitulo] nvarchar(4000) NULL,
                                                       [DataLiberacao] datetime2 NOT NULL,
                                                       [Ativo] bit NOT NULL,
                                                       [CreatedAt] datetime2 NOT NULL,
                                                       [UpdatedAt] datetime2 NULL,
                                                       CONSTRAINT [PK_DropConfig] PRIMARY KEY ([Id])
                                                   );
                                               END
                                               """);
            return;
        }

        if (dataContext.Database.IsNpgsql())
        {
            dataContext.Database.ExecuteSqlRaw("""
                                               CREATE TABLE IF NOT EXISTS "DropConfig" (
                                                   "Id" character varying(50) NOT NULL,
                                                   "IsDeleted" boolean NOT NULL DEFAULT FALSE,
                                                   "CreatedAtUtc" timestamp with time zone NULL,
                                                   "CreatedById" character varying(450) NULL,
                                                   "UpdatedAtUtc" timestamp with time zone NULL,
                                                   "UpdatedById" character varying(450) NULL,
                                                   "Titulo" character varying(255) NOT NULL,
                                                   "Subtitulo" character varying(4000) NULL,
                                                   "DataLiberacao" timestamp with time zone NOT NULL,
                                                   "Ativo" boolean NOT NULL,
                                                   "CreatedAt" timestamp with time zone NOT NULL,
                                                   "UpdatedAt" timestamp with time zone NULL,
                                                   CONSTRAINT "PK_DropConfig" PRIMARY KEY ("Id")
                                               );
                                               """);
        }
    }

    private static void EnsurePaidBagProductsUnavailable(DataContext dataContext)
    {
        if (dataContext.Database.IsSqlServer())
        {
            dataContext.Database.ExecuteSqlRaw("""
                                               UPDATE p
                                               SET p.ProductAvailable = 0
                                               FROM [Product] p
                                               WHERE (p.IsDeleted = 0 OR p.IsDeleted IS NULL)
                                                 AND (p.ProductAvailable = 1 OR p.ProductAvailable IS NULL)
                                                 AND EXISTS (
                                                     SELECT 1
                                                     FROM [BagItem] bi
                                                     INNER JOIN [Bag] b ON b.Id = bi.BagId
                                                     WHERE bi.ProductId = p.Id
                                                       AND (bi.IsDeleted = 0 OR bi.IsDeleted IS NULL)
                                                       AND (b.IsDeleted = 0 OR b.IsDeleted IS NULL)
                                                       AND b.AllItemsPaid = 1
                                                 );
                                               """);
            return;
        }

        if (dataContext.Database.IsNpgsql())
        {
            dataContext.Database.ExecuteSqlRaw("""
                                               UPDATE "Product" p
                                               SET "ProductAvailable" = FALSE
                                               WHERE (p."IsDeleted" = FALSE OR p."IsDeleted" IS NULL)
                                                 AND (p."ProductAvailable" = TRUE OR p."ProductAvailable" IS NULL)
                                                 AND EXISTS (
                                                     SELECT 1
                                                     FROM "BagItem" bi
                                                     INNER JOIN "Bag" b ON b."Id" = bi."BagId"
                                                     WHERE bi."ProductId" = p."Id"
                                                       AND (bi."IsDeleted" = FALSE OR bi."IsDeleted" IS NULL)
                                                       AND (b."IsDeleted" = FALSE OR b."IsDeleted" IS NULL)
                                                       AND b."AllItemsPaid" = TRUE
                                                 );
                                               """);
        }
    }

    private static void EnsurePaidOrderProductsUnavailable(DataContext dataContext)
    {
        const int paidStatus = 1;

        if (dataContext.Database.IsSqlServer())
        {
            dataContext.Database.ExecuteSqlRaw("""
                                               UPDATE p
                                               SET p.ProductAvailable = 0
                                               FROM [Product] p
                                               WHERE (p.IsDeleted = 0 OR p.IsDeleted IS NULL)
                                                 AND (p.ProductAvailable = 1 OR p.ProductAvailable IS NULL)
                                                 AND EXISTS (
                                                     SELECT 1
                                                     FROM [OrderDetail] od
                                                     INNER JOIN [Order] o ON o.Id = od.OrderId
                                                     WHERE od.ProductId = p.Id
                                                       AND (od.IsDeleted = 0 OR od.IsDeleted IS NULL)
                                                       AND (o.IsDeleted = 0 OR o.IsDeleted IS NULL)
                                                       AND o.Status = {0}
                                                 );
                                               """, paidStatus);
            return;
        }

        if (dataContext.Database.IsNpgsql())
        {
            dataContext.Database.ExecuteSqlRaw("""
                                               UPDATE "Product" p
                                               SET "ProductAvailable" = FALSE
                                               WHERE (p."IsDeleted" = FALSE OR p."IsDeleted" IS NULL)
                                                 AND (p."ProductAvailable" = TRUE OR p."ProductAvailable" IS NULL)
                                                 AND EXISTS (
                                                     SELECT 1
                                                     FROM "OrderDetail" od
                                                     INNER JOIN "Order" o ON o."Id" = od."OrderId"
                                                     WHERE od."ProductId" = p."Id"
                                                       AND (od."IsDeleted" = FALSE OR od."IsDeleted" IS NULL)
                                                       AND (o."IsDeleted" = FALSE OR o."IsDeleted" IS NULL)
                                                       AND o."Status" = {0}
                                                 );
                                               """, paidStatus);
        }
    }

    private static void EnsureOrderMelhorEnvioCartColumns(DataContext dataContext)
    {
        if (dataContext.Database.IsSqlServer())
        {
            dataContext.Database.ExecuteSqlRaw("""
                                               IF COL_LENGTH('dbo.[Order]', 'MelhorEnvioCartId') IS NULL
                                               BEGIN
                                                   ALTER TABLE [Order] ADD [MelhorEnvioCartId] nvarchar(max) NULL;
                                               END
                                               """);

            dataContext.Database.ExecuteSqlRaw("""
                                               IF COL_LENGTH('dbo.[Order]', 'MelhorEnvioCartAddedAt') IS NULL
                                               BEGIN
                                                   ALTER TABLE [Order] ADD [MelhorEnvioCartAddedAt] datetime2 NULL;
                                               END
                                               """);

            dataContext.Database.ExecuteSqlRaw("""
                                               IF COL_LENGTH('dbo.[Order]', 'MelhorEnvioCheckoutAt') IS NULL
                                               BEGIN
                                                   ALTER TABLE [Order] ADD [MelhorEnvioCheckoutAt] datetime2 NULL;
                                               END
                                               """);

            dataContext.Database.ExecuteSqlRaw("""
                                               IF COL_LENGTH('dbo.[Order]', 'MelhorEnvioGeneratedAt') IS NULL
                                               BEGIN
                                                   ALTER TABLE [Order] ADD [MelhorEnvioGeneratedAt] datetime2 NULL;
                                               END
                                               """);
            return;
        }

        if (dataContext.Database.IsNpgsql())
        {
            dataContext.Database.ExecuteSqlRaw("""
                                               ALTER TABLE "Order"
                                               ADD COLUMN IF NOT EXISTS "MelhorEnvioCartId" text;
                                               """);

            dataContext.Database.ExecuteSqlRaw("""
                                               ALTER TABLE "Order"
                                               ADD COLUMN IF NOT EXISTS "MelhorEnvioCartAddedAt" timestamp with time zone;
                                               """);

            dataContext.Database.ExecuteSqlRaw("""
                                               ALTER TABLE "Order"
                                               ADD COLUMN IF NOT EXISTS "MelhorEnvioCheckoutAt" timestamp with time zone;
                                               """);

            dataContext.Database.ExecuteSqlRaw("""
                                               ALTER TABLE "Order"
                                               ADD COLUMN IF NOT EXISTS "MelhorEnvioGeneratedAt" timestamp with time zone;
                                               """);
        }
    }

    private static void EnsureCustomerTable(DataContext dataContext)
    {
        if (dataContext.Database.IsSqlServer())
        {
            dataContext.Database.ExecuteSqlRaw("""
                                               IF COL_LENGTH('dbo.Customer', 'PasswordHash') IS NULL
                                               BEGIN
                                                   ALTER TABLE [Customer] ADD [PasswordHash] nvarchar(512) NULL;
                                               END
                                               """);
            return;
        }

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
                                          "PasswordHash" character varying(512),
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

        const string alterTableSql = """
                                     ALTER TABLE "Customer"
                                     ADD COLUMN IF NOT EXISTS "PasswordHash" character varying(512);
                                     """;

        dataContext.Database.ExecuteSqlRaw(createTableSql);
        dataContext.Database.ExecuteSqlRaw(alterTableSql);
        dataContext.Database.ExecuteSqlRaw(createIndexesSql);
    }
}

