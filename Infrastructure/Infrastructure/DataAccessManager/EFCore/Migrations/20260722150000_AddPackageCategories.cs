using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.DataAccessManager.EFCore.Migrations;

public partial class AddPackageCategories : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "PackageCategory",
            columns: table => new
            {
                Id = table.Column<string>(maxLength: 50, nullable: false),
                IsDeleted = table.Column<bool>(nullable: false),
                CreatedAtUtc = table.Column<DateTime>(nullable: true),
                CreatedById = table.Column<string>(nullable: true),
                UpdatedAtUtc = table.Column<DateTime>(nullable: true),
                UpdatedById = table.Column<string>(nullable: true),
                Name = table.Column<string>(maxLength: 150, nullable: false),
                CapacityPoints = table.Column<int>(nullable: false),
                Description = table.Column<string>(maxLength: 500, nullable: true),
                IsActive = table.Column<bool>(nullable: false),
                CreatedAt = table.Column<DateTime>(nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_PackageCategory", x => x.Id));
        migrationBuilder.CreateIndex("IX_PackageCategory_Name", "PackageCategory", "Name", unique: true);
        migrationBuilder.AddColumn<string>("PackageCategoryId", "ShippingBox", maxLength: 50, nullable: true);
        migrationBuilder.CreateIndex("IX_ShippingBox_PackageCategoryId", "ShippingBox", "PackageCategoryId");
        migrationBuilder.AddForeignKey("FK_ShippingBox_PackageCategory_PackageCategoryId", "ShippingBox", "PackageCategoryId", "PackageCategory", "Id", onDelete: ReferentialAction.Restrict);

        var now = DateTime.UtcNow;
        migrationBuilder.InsertData("PackageCategory",
            new[] { "Id", "IsDeleted", "Name", "CapacityPoints", "Description", "IsActive", "CreatedAt" },
            new object[,]
            {
                { "pkg-envelope-p", false, "Envelope P", 1, null, true, now },
                { "pkg-envelope-m", false, "Envelope M", 3, null, true, now },
                { "pkg-caixa-p", false, "Caixa P", 5, null, true, now },
                { "pkg-caixa-m", false, "Caixa M", 8, null, true, now },
                { "pkg-caixa-g", false, "Caixa G", 12, null, true, now }
            });
        if (ActiveProvider.Contains("SqlServer"))
        {
            migrationBuilder.Sql("""
                UPDATE box SET [PackageCategoryId] = category.[Id]
                FROM [ShippingBox] box
                CROSS APPLY (SELECT TOP 1 [Id] FROM [PackageCategory] WHERE [CapacityPoints] = box.[CapacityPoints] ORDER BY [Name]) category
                WHERE box.[PackageCategoryId] IS NULL;
                """);
        }
        else
        {
            migrationBuilder.Sql("""
                UPDATE "ShippingBox" SET "PackageCategoryId" = (
                    SELECT "Id" FROM "PackageCategory"
                    WHERE "PackageCategory"."CapacityPoints" = "ShippingBox"."CapacityPoints"
                    ORDER BY "Name" LIMIT 1)
                WHERE "PackageCategoryId" IS NULL;
                """);
        }
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey("FK_ShippingBox_PackageCategory_PackageCategoryId", "ShippingBox");
        migrationBuilder.DropIndex("IX_ShippingBox_PackageCategoryId", "ShippingBox");
        migrationBuilder.DropColumn("PackageCategoryId", "ShippingBox");
        migrationBuilder.DropTable("PackageCategory");
    }
}
