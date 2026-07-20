using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.DataAccessManager.EFCore.Migrations;

public partial class AddOrderDetailProductSnapshot : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ProductImageUrl",
            table: "OrderDetail",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ProductName",
            table: "OrderDetail",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ProductImageUrl",
            table: "OrderDetail");

        migrationBuilder.DropColumn(
            name: "ProductName",
            table: "OrderDetail");
    }
}
