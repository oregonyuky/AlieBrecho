using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.DataAccessManager.EFCore.Migrations;

[DbContext(typeof(DataContext))]
[Migration("20260721000000_AddProductConcurrencyAndActiveReservationConstraint")]
public partial class AddProductConcurrencyAndActiveReservationConstraint : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        if (ActiveProvider.Contains("SqlServer", StringComparison.OrdinalIgnoreCase))
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Product",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.Sql("""
                UPDATE [BagItem]
                SET [IsReserved] = 0
                WHERE [IsReserved] = 1
                  AND ([IsDeleted] = 1 OR [IsPaid] = 1 OR ([ReservationExpiresAt] IS NOT NULL AND [ReservationExpiresAt] <= SYSUTCDATETIME()));
                """);
        }
        else if (ActiveProvider.Contains("Npgsql", StringComparison.OrdinalIgnoreCase))
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Product",
                type: "bytea",
                nullable: false,
                defaultValue: Array.Empty<byte>());

            migrationBuilder.Sql("""
                UPDATE "BagItem"
                SET "IsReserved" = FALSE
                WHERE "IsReserved"
                  AND ("IsDeleted" OR "IsPaid" OR ("ReservationExpiresAt" IS NOT NULL AND "ReservationExpiresAt" <= CURRENT_TIMESTAMP));
                """);
        }
        else
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Product",
                type: "BLOB",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "BagItem"
                SET "IsReserved" = 0
                WHERE "IsReserved" = 1
                  AND ("IsDeleted" = 1 OR "IsPaid" = 1 OR ("ReservationExpiresAt" IS NOT NULL AND "ReservationExpiresAt" <= CURRENT_TIMESTAMP));
                """);
        }

        if (ActiveProvider.Contains("Npgsql", StringComparison.OrdinalIgnoreCase))
        {
            migrationBuilder.Sql("""
                WITH "DuplicateReservations" AS (
                    SELECT "Id", ROW_NUMBER() OVER (PARTITION BY "ProductId" ORDER BY "AddedAt", "Id") AS "RowNumber"
                    FROM "BagItem"
                    WHERE "ProductId" IS NOT NULL AND NOT "IsDeleted" AND "IsReserved"
                )
                UPDATE "BagItem"
                SET "IsReserved" = FALSE
                WHERE "Id" IN (SELECT "Id" FROM "DuplicateReservations" WHERE "RowNumber" > 1);
                """);
        }
        else
        {
            migrationBuilder.Sql("""
                WITH DuplicateReservations AS (
                    SELECT "Id", ROW_NUMBER() OVER (PARTITION BY "ProductId" ORDER BY "AddedAt", "Id") AS "RowNumber"
                    FROM "BagItem"
                    WHERE "ProductId" IS NOT NULL AND "IsDeleted" = 0 AND "IsReserved" = 1
                )
                UPDATE "BagItem"
                SET "IsReserved" = 0
                WHERE "Id" IN (SELECT "Id" FROM DuplicateReservations WHERE "RowNumber" > 1);
                """);
        }

        var indexFilter = ActiveProvider.Contains("Npgsql", StringComparison.OrdinalIgnoreCase)
            ? "\"ProductId\" IS NOT NULL AND NOT \"IsDeleted\" AND \"IsReserved\""
            : ActiveProvider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase)
                ? "\"ProductId\" IS NOT NULL AND \"IsDeleted\" = 0 AND \"IsReserved\" = 1"
                : "[ProductId] IS NOT NULL AND [IsDeleted] = 0 AND [IsReserved] = 1";

        migrationBuilder.CreateIndex(
            name: "UX_BagItem_ActiveReservation_ProductId",
            table: "BagItem",
            column: "ProductId",
            unique: true,
            filter: indexFilter);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "UX_BagItem_ActiveReservation_ProductId",
            table: "BagItem");

        migrationBuilder.DropColumn(
            name: "RowVersion",
            table: "Product");
    }
}
