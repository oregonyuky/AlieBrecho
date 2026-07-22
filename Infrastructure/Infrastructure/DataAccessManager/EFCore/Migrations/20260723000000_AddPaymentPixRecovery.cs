using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.DataAccessManager.EFCore.Migrations;

[DbContext(typeof(DataContext))]
[Migration("20260723000000_AddPaymentPixRecovery")]
public partial class AddPaymentPixRecovery : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "PixQrCodeBase64", table: "Payment", nullable: true);
        migrationBuilder.AddColumn<string>(name: "PixQrCode", table: "Payment", nullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "ExpiresAt", table: "Payment", nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "PixQrCodeBase64", table: "Payment");
        migrationBuilder.DropColumn(name: "PixQrCode", table: "Payment");
        migrationBuilder.DropColumn(name: "ExpiresAt", table: "Payment");
    }
}
