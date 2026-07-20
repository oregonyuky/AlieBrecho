using Application.Common.CQS.Commands;
using Application.Common.CQS.Queries;
using Application.Common.Repositories;
using System.Data;
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
            case "Sqlite":
                services.AddDbContext<DataContext>(options =>
                    options.UseSqlite(connectionString)
                    .LogTo(Log.Information, LogLevel.Information)
                    .EnableSensitiveDataLogging()
                );
                services.AddDbContext<CommandContext>(options =>
                    options.UseSqlite(connectionString)
                    .LogTo(Log.Information, LogLevel.Information)
                    .EnableSensitiveDataLogging()
                );
                services.AddDbContext<QueryContext>(options =>
                    options.UseSqlite(connectionString)
                    .LogTo(Log.Information, LogLevel.Information)
                    .EnableSensitiveDataLogging()
                );
                break;
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
        EnsureOrderDetailSnapshotColumns(dataContext);
        EnsurePaidOrderProductsUnavailable(dataContext);
        EnsurePaidBagProductsUnavailable(dataContext);
        EnsureInfinitePayPaymentColumns(dataContext);
        EnsureDropConfigTable(dataContext);
        EnsureProductDropConfigColumn(dataContext);
        EnsureProductSizeStockQuantityColumn(dataContext);
        EnsureBagSettingsTable(dataContext);
        EnsureBagExpirationHistoryTable(dataContext);
        EnsureContactMessageTable(dataContext);

        return host;
    }

    private static void EnsureBagSettingsTable(DataContext dataContext)
    {
        if (dataContext.Database.IsSqlServer())
        {
            dataContext.Database.ExecuteSqlRaw("""
                                               IF OBJECT_ID('dbo.BagSettings', 'U') IS NULL
                                               BEGIN
                                                   CREATE TABLE [BagSettings] (
                                                       [Id] nvarchar(50) NOT NULL,
                                                       [IsDeleted] bit NOT NULL CONSTRAINT [DF_BagSettings_IsDeleted] DEFAULT CAST(0 AS bit),
                                                       [CreatedAtUtc] datetime2 NULL,
                                                       [CreatedById] nvarchar(450) NULL,
                                                       [UpdatedAtUtc] datetime2 NULL,
                                                       [UpdatedById] nvarchar(450) NULL,
                                                       [DefaultDurationValue] int NOT NULL,
                                                       [DefaultDurationUnit] nvarchar(20) NOT NULL,
                                                       [ExtensionDurationValue] int NOT NULL,
                                                       [ExtensionDurationUnit] nvarchar(20) NOT NULL,
                                                       [ExtensionResponseDeadlineValue] int NOT NULL,
                                                       [ExtensionResponseDeadlineUnit] nvarchar(20) NOT NULL,
                                                       CONSTRAINT [PK_BagSettings] PRIMARY KEY ([Id])
                                                   );
                                               END
                                               """);
            return;
        }

        if (dataContext.Database.IsNpgsql())
        {
            dataContext.Database.ExecuteSqlRaw("""
                                               CREATE TABLE IF NOT EXISTS "BagSettings" (
                                                   "Id" character varying(50) NOT NULL,
                                                   "IsDeleted" boolean NOT NULL DEFAULT FALSE,
                                                   "CreatedAtUtc" timestamp with time zone NULL,
                                                   "CreatedById" character varying(450) NULL,
                                                   "UpdatedAtUtc" timestamp with time zone NULL,
                                                   "UpdatedById" character varying(450) NULL,
                                                   "DefaultDurationValue" integer NOT NULL,
                                                   "DefaultDurationUnit" character varying(20) NOT NULL,
                                                   "ExtensionDurationValue" integer NOT NULL,
                                                   "ExtensionDurationUnit" character varying(20) NOT NULL,
                                                   "ExtensionResponseDeadlineValue" integer NOT NULL,
                                                   "ExtensionResponseDeadlineUnit" character varying(20) NOT NULL,
                                                   CONSTRAINT "PK_BagSettings" PRIMARY KEY ("Id")
                                               );
                                               """);
            return;
        }

        if (dataContext.Database.IsSqlite())
        {
            dataContext.Database.ExecuteSqlRaw("""
                                               CREATE TABLE IF NOT EXISTS "BagSettings" (
                                                   "Id" TEXT NOT NULL CONSTRAINT "PK_BagSettings" PRIMARY KEY,
                                                   "IsDeleted" INTEGER NOT NULL DEFAULT 0,
                                                   "CreatedAtUtc" TEXT NULL,
                                                   "CreatedById" TEXT NULL,
                                                   "UpdatedAtUtc" TEXT NULL,
                                                   "UpdatedById" TEXT NULL,
                                                   "DefaultDurationValue" INTEGER NOT NULL,
                                                   "DefaultDurationUnit" TEXT NOT NULL,
                                                   "ExtensionDurationValue" INTEGER NOT NULL,
                                                   "ExtensionDurationUnit" TEXT NOT NULL,
                                                   "ExtensionResponseDeadlineValue" INTEGER NOT NULL,
                                                   "ExtensionResponseDeadlineUnit" TEXT NOT NULL
                                               );
                                               """);
        }
    }

    private static void EnsureBagExpirationHistoryTable(DataContext dataContext)
    {
        if (dataContext.Database.IsSqlServer())
        {
            dataContext.Database.ExecuteSqlRaw("""
                                               IF OBJECT_ID('dbo.BagExpirationHistory', 'U') IS NULL
                                               BEGIN
                                                   CREATE TABLE [BagExpirationHistory] (
                                                       [Id] nvarchar(50) NOT NULL,
                                                       [IsDeleted] bit NOT NULL CONSTRAINT [DF_BagExpirationHistory_IsDeleted] DEFAULT CAST(0 AS bit),
                                                       [CreatedAtUtc] datetime2 NULL,
                                                       [CreatedById] nvarchar(450) NULL,
                                                       [UpdatedAtUtc] datetime2 NULL,
                                                       [UpdatedById] nvarchar(450) NULL,
                                                       [BagId] nvarchar(50) NOT NULL,
                                                       [OldExpirationDate] datetime2 NOT NULL,
                                                       [NewExpirationDate] datetime2 NOT NULL,
                                                       [ChangedBy] nvarchar(255) NULL,
                                                       [ChangedAtUtc] datetime2 NOT NULL,
                                                       [Note] nvarchar(1000) NULL,
                                                       CONSTRAINT [PK_BagExpirationHistory] PRIMARY KEY ([Id]),
                                                       CONSTRAINT [FK_BagExpirationHistory_Bag_BagId] FOREIGN KEY ([BagId]) REFERENCES [Bag] ([Id]) ON DELETE CASCADE
                                                   );
                                                   CREATE INDEX [IX_BagExpirationHistory_BagId] ON [BagExpirationHistory] ([BagId]);
                                               END
                                               """);
            return;
        }

        if (dataContext.Database.IsNpgsql())
        {
            dataContext.Database.ExecuteSqlRaw("""
                                               CREATE TABLE IF NOT EXISTS "BagExpirationHistory" (
                                                   "Id" character varying(50) NOT NULL,
                                                   "IsDeleted" boolean NOT NULL DEFAULT FALSE,
                                                   "CreatedAtUtc" timestamp with time zone NULL,
                                                   "CreatedById" character varying(450) NULL,
                                                   "UpdatedAtUtc" timestamp with time zone NULL,
                                                   "UpdatedById" character varying(450) NULL,
                                                   "BagId" character varying(50) NOT NULL,
                                                   "OldExpirationDate" timestamp with time zone NOT NULL,
                                                   "NewExpirationDate" timestamp with time zone NOT NULL,
                                                   "ChangedBy" character varying(255) NULL,
                                                   "ChangedAtUtc" timestamp with time zone NOT NULL,
                                                   "Note" character varying(1000) NULL,
                                                   CONSTRAINT "PK_BagExpirationHistory" PRIMARY KEY ("Id"),
                                                   CONSTRAINT "FK_BagExpirationHistory_Bag_BagId" FOREIGN KEY ("BagId") REFERENCES "Bag" ("Id") ON DELETE CASCADE
                                               );

                                               CREATE INDEX IF NOT EXISTS "IX_BagExpirationHistory_BagId" ON "BagExpirationHistory" ("BagId");
                                               """);
            return;
        }

        if (dataContext.Database.IsSqlite())
        {
            dataContext.Database.ExecuteSqlRaw("""
                                               CREATE TABLE IF NOT EXISTS "BagExpirationHistory" (
                                                   "Id" TEXT NOT NULL CONSTRAINT "PK_BagExpirationHistory" PRIMARY KEY,
                                                   "IsDeleted" INTEGER NOT NULL DEFAULT 0,
                                                   "CreatedAtUtc" TEXT NULL,
                                                   "CreatedById" TEXT NULL,
                                                   "UpdatedAtUtc" TEXT NULL,
                                                   "UpdatedById" TEXT NULL,
                                                   "BagId" TEXT NOT NULL,
                                                   "OldExpirationDate" TEXT NOT NULL,
                                                   "NewExpirationDate" TEXT NOT NULL,
                                                   "ChangedBy" TEXT NULL,
                                                   "ChangedAtUtc" TEXT NOT NULL,
                                                   "Note" TEXT NULL,
                                                   CONSTRAINT "FK_BagExpirationHistory_Bag_BagId" FOREIGN KEY ("BagId") REFERENCES "Bag" ("Id") ON DELETE CASCADE
                                               );
                                               """);
            dataContext.Database.ExecuteSqlRaw("""
                                               CREATE INDEX IF NOT EXISTS "IX_BagExpirationHistory_BagId" ON "BagExpirationHistory" ("BagId");
                                               """);
        }
    }

    private static void EnsureProductSizeStockQuantityColumn(DataContext dataContext)
    {
        if (dataContext.Database.IsSqlServer())
        {
            dataContext.Database.ExecuteSqlRaw("""
                                               IF COL_LENGTH('dbo.ProductSize', 'StockQuantity') IS NULL
                                               BEGIN
                                                   ALTER TABLE [ProductSize] ADD [StockQuantity] int NULL;
                                               END
                                               """);
            return;
        }

        if (dataContext.Database.IsNpgsql())
        {
            dataContext.Database.ExecuteSqlRaw("""
                                               ALTER TABLE "ProductSize"
                                               ADD COLUMN IF NOT EXISTS "StockQuantity" integer;
                                               """);
            return;
        }

        if (dataContext.Database.IsSqlite() && !SqliteColumnExists(dataContext, "ProductSize", "StockQuantity"))
        {
            dataContext.Database.ExecuteSqlRaw("""
                                               ALTER TABLE "ProductSize" ADD COLUMN "StockQuantity" INTEGER NULL;
                                               """);
        }
    }

    private static void EnsureProductDropConfigColumn(DataContext dataContext)
    {
        if (dataContext.Database.IsSqlServer())
        {
            dataContext.Database.ExecuteSqlRaw("""
                                               IF COL_LENGTH('dbo.Product', 'DropConfigId') IS NULL
                                               BEGIN
                                                   ALTER TABLE [Product] ADD [DropConfigId] nvarchar(50) NULL;
                                               END

                                               IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Product_DropConfigId' AND object_id = OBJECT_ID('dbo.Product'))
                                               BEGIN
                                                   CREATE INDEX [IX_Product_DropConfigId] ON [Product] ([DropConfigId]);
                                               END

                                               IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Product_DropConfig_DropConfigId')
                                               BEGIN
                                                   ALTER TABLE [Product]
                                                   ADD CONSTRAINT [FK_Product_DropConfig_DropConfigId]
                                                   FOREIGN KEY ([DropConfigId]) REFERENCES [DropConfig] ([Id])
                                                   ON DELETE SET NULL;
                                               END
                                               """);
            return;
        }

        if (dataContext.Database.IsNpgsql())
        {
            dataContext.Database.ExecuteSqlRaw("""
                                               ALTER TABLE "Product"
                                               ADD COLUMN IF NOT EXISTS "DropConfigId" character varying(50);

                                               CREATE INDEX IF NOT EXISTS "IX_Product_DropConfigId" ON "Product" ("DropConfigId");

                                               DO $$
                                               BEGIN
                                                   IF NOT EXISTS (
                                                       SELECT 1
                                                       FROM pg_constraint
                                                       WHERE conname = 'FK_Product_DropConfig_DropConfigId'
                                                   ) THEN
                                                       ALTER TABLE "Product"
                                                       ADD CONSTRAINT "FK_Product_DropConfig_DropConfigId"
                                                       FOREIGN KEY ("DropConfigId") REFERENCES "DropConfig" ("Id")
                                                       ON DELETE SET NULL;
                                                   END IF;
                                               END $$;
                                               """);
            return;
        }

        if (dataContext.Database.IsSqlite() && !SqliteColumnExists(dataContext, "Product", "DropConfigId"))
        {
            dataContext.Database.ExecuteSqlRaw("""
                                               ALTER TABLE "Product" ADD COLUMN "DropConfigId" TEXT NULL;
                                               """);
        }
    }

    private static bool SqliteColumnExists(DataContext dataContext, string tableName, string columnName)
    {
        var connection = dataContext.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;

        if (shouldClose)
        {
            connection.Open();
        }

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = $"""PRAGMA table_info("{tableName}");""";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                if (string.Equals(reader["name"]?.ToString(), columnName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
        finally
        {
            if (shouldClose)
            {
                connection.Close();
            }
        }
    }

    private static void EnsureInfinitePayPaymentColumns(DataContext dataContext)
    {
        if (dataContext.Database.IsSqlServer())
        {
            foreach (var sql in new[]
            {
                "IF COL_LENGTH('dbo.Payment', 'Provider') IS NULL BEGIN ALTER TABLE [Payment] ADD [Provider] nvarchar(100) NULL; END",
                "IF COL_LENGTH('dbo.Payment', 'CheckoutUrl') IS NULL BEGIN ALTER TABLE [Payment] ADD [CheckoutUrl] nvarchar(max) NULL; END",
                "IF COL_LENGTH('dbo.Payment', 'ProviderTransactionId') IS NULL BEGIN ALTER TABLE [Payment] ADD [ProviderTransactionId] nvarchar(255) NULL; END",
                "IF COL_LENGTH('dbo.Payment', 'PaidAt') IS NULL BEGIN ALTER TABLE [Payment] ADD [PaidAt] datetime2 NULL; END"
            })
            {
                dataContext.Database.ExecuteSqlRaw(sql);
            }

            return;
        }

        if (dataContext.Database.IsNpgsql())
        {
            dataContext.Database.ExecuteSqlRaw("""
                                               ALTER TABLE "Payment" ADD COLUMN IF NOT EXISTS "Provider" character varying(100);
                                               ALTER TABLE "Payment" ADD COLUMN IF NOT EXISTS "CheckoutUrl" text;
                                               ALTER TABLE "Payment" ADD COLUMN IF NOT EXISTS "ProviderTransactionId" character varying(255);
                                               ALTER TABLE "Payment" ADD COLUMN IF NOT EXISTS "PaidAt" timestamp with time zone;
                                               """);
            return;
        }

        if (dataContext.Database.IsSqlite())
        {
            if (!SqliteColumnExists(dataContext, "Payment", "Provider"))
            {
                dataContext.Database.ExecuteSqlRaw("""ALTER TABLE "Payment" ADD COLUMN "Provider" TEXT NULL;""");
            }

            if (!SqliteColumnExists(dataContext, "Payment", "CheckoutUrl"))
            {
                dataContext.Database.ExecuteSqlRaw("""ALTER TABLE "Payment" ADD COLUMN "CheckoutUrl" TEXT NULL;""");
            }

            if (!SqliteColumnExists(dataContext, "Payment", "ProviderTransactionId"))
            {
                dataContext.Database.ExecuteSqlRaw("""ALTER TABLE "Payment" ADD COLUMN "ProviderTransactionId" TEXT NULL;""");
            }

            if (!SqliteColumnExists(dataContext, "Payment", "PaidAt"))
            {
                dataContext.Database.ExecuteSqlRaw("""ALTER TABLE "Payment" ADD COLUMN "PaidAt" TEXT NULL;""");
            }
        }
    }

    private static void EnsureContactMessageTable(DataContext dataContext)
    {
        if (dataContext.Database.IsSqlServer())
        {
            dataContext.Database.ExecuteSqlRaw("""
                                               IF OBJECT_ID('dbo.ContactMessage', 'U') IS NULL
                                               BEGIN
                                                   CREATE TABLE [ContactMessage] (
                                                       [Id] nvarchar(50) NOT NULL,
                                                       [IsDeleted] bit NOT NULL CONSTRAINT [DF_ContactMessage_IsDeleted] DEFAULT CAST(0 AS bit),
                                                       [CreatedAtUtc] datetime2 NULL,
                                                       [CreatedById] nvarchar(450) NULL,
                                                       [UpdatedAtUtc] datetime2 NULL,
                                                       [UpdatedById] nvarchar(450) NULL,
                                                       [Name] nvarchar(120) NOT NULL,
                                                       [Email] nvarchar(180) NOT NULL,
                                                       [Phone] nvarchar(30) NOT NULL,
                                                       [Subject] nvarchar(160) NOT NULL,
                                                       [Message] nvarchar(4000) NOT NULL,
                                                       [IsRead] bit NOT NULL CONSTRAINT [DF_ContactMessage_IsRead] DEFAULT CAST(0 AS bit),
                                                       [ReceivedAtUtc] datetime2 NOT NULL,
                                                       [ReadAtUtc] datetime2 NULL,
                                                       CONSTRAINT [PK_ContactMessage] PRIMARY KEY ([Id])
                                                   );
                                                   CREATE INDEX [IX_ContactMessage_IsRead_ReceivedAtUtc]
                                                       ON [ContactMessage] ([IsRead], [ReceivedAtUtc]);
                                               END
                                               """);
            return;
        }

        if (dataContext.Database.IsNpgsql())
        {
            dataContext.Database.ExecuteSqlRaw("""
                                               CREATE TABLE IF NOT EXISTS "ContactMessage" (
                                                   "Id" character varying(50) NOT NULL,
                                                   "IsDeleted" boolean NOT NULL DEFAULT FALSE,
                                                   "CreatedAtUtc" timestamp with time zone NULL,
                                                   "CreatedById" character varying(450) NULL,
                                                   "UpdatedAtUtc" timestamp with time zone NULL,
                                                   "UpdatedById" character varying(450) NULL,
                                                   "Name" character varying(120) NOT NULL,
                                                   "Email" character varying(180) NOT NULL,
                                                   "Phone" character varying(30) NOT NULL,
                                                   "Subject" character varying(160) NOT NULL,
                                                   "Message" character varying(4000) NOT NULL,
                                                   "IsRead" boolean NOT NULL DEFAULT FALSE,
                                                   "ReceivedAtUtc" timestamp with time zone NOT NULL,
                                                   "ReadAtUtc" timestamp with time zone NULL,
                                                   CONSTRAINT "PK_ContactMessage" PRIMARY KEY ("Id")
                                               );
                                               CREATE INDEX IF NOT EXISTS "IX_ContactMessage_IsRead_ReceivedAtUtc"
                                                   ON "ContactMessage" ("IsRead", "ReceivedAtUtc");
                                               """);
            return;
        }

        if (dataContext.Database.IsSqlite())
        {
            dataContext.Database.ExecuteSqlRaw("""
                                               CREATE TABLE IF NOT EXISTS "ContactMessage" (
                                                   "Id" TEXT NOT NULL CONSTRAINT "PK_ContactMessage" PRIMARY KEY,
                                                   "IsDeleted" INTEGER NOT NULL DEFAULT 0,
                                                   "CreatedAtUtc" TEXT NULL,
                                                   "CreatedById" TEXT NULL,
                                                   "UpdatedAtUtc" TEXT NULL,
                                                   "UpdatedById" TEXT NULL,
                                                   "Name" TEXT NOT NULL,
                                                   "Email" TEXT NOT NULL,
                                                   "Phone" TEXT NOT NULL,
                                                   "Subject" TEXT NOT NULL,
                                                   "Message" TEXT NOT NULL,
                                                   "IsRead" INTEGER NOT NULL DEFAULT 0,
                                                   "ReceivedAtUtc" TEXT NOT NULL,
                                                   "ReadAtUtc" TEXT NULL
                                               );
                                               CREATE INDEX IF NOT EXISTS "IX_ContactMessage_IsRead_ReceivedAtUtc"
                                                   ON "ContactMessage" ("IsRead", "ReceivedAtUtc");
                                               """);
        }
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

    private static void EnsureOrderDetailSnapshotColumns(DataContext dataContext)
    {
        if (dataContext.Database.IsSqlServer())
        {
            dataContext.Database.ExecuteSqlRaw("""
                                               IF COL_LENGTH('dbo.OrderDetail', 'ProductName') IS NULL
                                               BEGIN
                                                   ALTER TABLE [OrderDetail] ADD [ProductName] nvarchar(max) NULL;
                                               END
                                               """);

            dataContext.Database.ExecuteSqlRaw("""
                                               IF COL_LENGTH('dbo.OrderDetail', 'ProductImageUrl') IS NULL
                                               BEGIN
                                                   ALTER TABLE [OrderDetail] ADD [ProductImageUrl] nvarchar(max) NULL;
                                               END
                                               """);
            return;
        }

        if (dataContext.Database.IsNpgsql())
        {
            dataContext.Database.ExecuteSqlRaw("""
                                               ALTER TABLE "OrderDetail"
                                               ADD COLUMN IF NOT EXISTS "ProductName" text NULL;
                                               """);

            dataContext.Database.ExecuteSqlRaw("""
                                               ALTER TABLE "OrderDetail"
                                               ADD COLUMN IF NOT EXISTS "ProductImageUrl" text NULL;
                                               """);
            return;
        }

        if (dataContext.Database.IsSqlite())
        {
            if (!SqliteColumnExists(dataContext, "OrderDetail", "ProductName"))
            {
                dataContext.Database.ExecuteSqlRaw("""ALTER TABLE "OrderDetail" ADD COLUMN "ProductName" TEXT NULL;""");
            }

            if (!SqliteColumnExists(dataContext, "OrderDetail", "ProductImageUrl"))
            {
                dataContext.Database.ExecuteSqlRaw("""ALTER TABLE "OrderDetail" ADD COLUMN "ProductImageUrl" TEXT NULL;""");
            }
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

