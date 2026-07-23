using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace Application.Tests;

public class PostgreSqlCompatibilityTests
{
    [Fact]
    public void Create_script_uses_postgresql_compatible_filter_and_row_version()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseNpgsql("Host=localhost;Database=aliebrecho_model_test;Username=test;Password=test")
            .Options;

        using var context = new DataContext(options);
        var script = context.Database.GenerateCreateScript();

        Assert.Contains(
            "WHERE \"ProductId\" IS NOT NULL AND NOT \"IsDeleted\" AND \"IsReserved\"",
            script);
        Assert.DoesNotContain("[ProductId]", script);
        Assert.DoesNotContain("nvarchar", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("BLOB", script, StringComparison.OrdinalIgnoreCase);

        var rowVersion = context.Model.FindEntityType(typeof(Product))!
            .FindProperty(nameof(Product.RowVersion))!;
        Assert.True(rowVersion.IsConcurrencyToken);
        Assert.Equal(ValueGenerated.Never, rowVersion.ValueGenerated);
    }

    [Fact]
    public void Migration_script_does_not_emit_sqlite_or_sql_server_syntax_for_postgresql()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseNpgsql("Host=localhost;Database=aliebrecho_migration_test;Username=test;Password=test")
            .Options;

        using var context = new DataContext(options);
        var script = context.GetService<IMigrator>().GenerateScript();

        Assert.Contains("RowVersion", script);
        Assert.Contains("UX_BagItem_ActiveReservation_ProductId", script);
        Assert.DoesNotContain("[ProductId]", script);
        Assert.DoesNotContain("nvarchar", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("BLOB", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SYSUTCDATETIME", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("CROSS APPLY", script, StringComparison.OrdinalIgnoreCase);
    }
}
