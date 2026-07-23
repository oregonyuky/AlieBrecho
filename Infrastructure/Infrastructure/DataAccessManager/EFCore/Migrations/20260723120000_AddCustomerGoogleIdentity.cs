using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.DataAccessManager.EFCore.Migrations;

[DbContext(typeof(DataContext))]
[Migration("20260723120000_AddCustomerGoogleIdentity")]
public partial class AddCustomerGoogleIdentity : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // SQLite deployments are upgraded by CreateDatabase/EnsureCustomerGoogleIdentity.
        // This also keeps partial legacy SQLite schemas migratable.
        if (ActiveProvider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        migrationBuilder.AddColumn<string>(
            name: "GoogleProviderUserId",
            table: "Customer",
            maxLength: 255,
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Customer_GoogleProviderUserId",
            table: "Customer",
            column: "GoogleProviderUserId",
            unique: true,
            filter: "\"GoogleProviderUserId\" IS NOT NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        if (ActiveProvider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        migrationBuilder.DropIndex(
            name: "IX_Customer_GoogleProviderUserId",
            table: "Customer");

        migrationBuilder.DropColumn(
            name: "GoogleProviderUserId",
            table: "Customer");
    }
}
