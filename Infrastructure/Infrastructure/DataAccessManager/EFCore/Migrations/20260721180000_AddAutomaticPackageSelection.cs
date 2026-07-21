using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.DataAccessManager.EFCore.Migrations;

public partial class AddAutomaticPackageSelection : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>("PackageOccupationPoints", "Category", nullable: false, defaultValue: 1);
        migrationBuilder.AddColumn<string>("Name", "ShippingBox", maxLength: 150, nullable: false, defaultValue: "Embalagem existente");
        migrationBuilder.AddColumn<int>("CapacityPoints", "ShippingBox", nullable: false, defaultValue: 1);
        migrationBuilder.AddColumn<int>("StockQuantity", "ShippingBox", nullable: false, defaultValue: 0);
        migrationBuilder.AddColumn<decimal>("MaxWeight", "ShippingBox", type: "decimal(10,3)", nullable: true);
        migrationBuilder.AddColumn<string>("PackageName", "Order", nullable: true);
        migrationBuilder.AddColumn<decimal>("PackageLength", "Order", nullable: true);
        migrationBuilder.AddColumn<decimal>("PackageWidth", "Order", nullable: true);
        migrationBuilder.AddColumn<decimal>("PackageHeight", "Order", nullable: true);
        migrationBuilder.AddColumn<decimal>("PackageWeight", "Order", nullable: true);
        migrationBuilder.AddColumn<int>("PackageCapacityPoints", "Order", nullable: true);
        migrationBuilder.AddColumn<int>("PackageOccupationPoints", "Order", nullable: true);
        migrationBuilder.AddColumn<bool>("ShippingBoxStockDeducted", "Order", nullable: false, defaultValue: false);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn("PackageOccupationPoints", "Category");
        migrationBuilder.DropColumn("Name", "ShippingBox");
        migrationBuilder.DropColumn("CapacityPoints", "ShippingBox");
        migrationBuilder.DropColumn("StockQuantity", "ShippingBox");
        migrationBuilder.DropColumn("MaxWeight", "ShippingBox");
        migrationBuilder.DropColumn("PackageName", "Order");
        migrationBuilder.DropColumn("PackageLength", "Order");
        migrationBuilder.DropColumn("PackageWidth", "Order");
        migrationBuilder.DropColumn("PackageHeight", "Order");
        migrationBuilder.DropColumn("PackageWeight", "Order");
        migrationBuilder.DropColumn("PackageCapacityPoints", "Order");
        migrationBuilder.DropColumn("PackageOccupationPoints", "Order");
        migrationBuilder.DropColumn("ShippingBoxStockDeducted", "Order");
    }
}
