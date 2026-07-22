using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.DataAccessManager.EFCore.Migrations;

[DbContext(typeof(DataContext))]
[Migration("20260722000000_AddBagCurrentPayment")]
public partial class AddBagCurrentPayment : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "CurrentPaymentId",
            table: "Bag",
            maxLength: 200,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "CurrentPaymentProvider",
            table: "Bag",
            maxLength: 50,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "CurrentPaymentQrCodeBase64",
            table: "Bag",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "CurrentPaymentQrCode",
            table: "Bag",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "CurrentPaymentExpiresAt",
            table: "Bag",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "CurrentPaymentId", table: "Bag");
        migrationBuilder.DropColumn(name: "CurrentPaymentProvider", table: "Bag");
        migrationBuilder.DropColumn(name: "CurrentPaymentQrCodeBase64", table: "Bag");
        migrationBuilder.DropColumn(name: "CurrentPaymentQrCode", table: "Bag");
        migrationBuilder.DropColumn(name: "CurrentPaymentExpiresAt", table: "Bag");
    }
}
