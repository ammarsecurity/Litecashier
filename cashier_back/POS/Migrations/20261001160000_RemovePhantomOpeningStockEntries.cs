using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using POS.Db;

#nullable disable

namespace POS.Migrations
{
    /// <summary>
    /// Removes phantom opening ledger rows that were seeded from Items.Quantity
    /// when no positive warehouse stock existed (desynced catalogs).
    /// </summary>
    [DbContext(typeof(DbConfig))]
    [Migration("20261001160000_RemovePhantomOpeningStockEntries")]
    public partial class RemovePhantomOpeningStockEntries : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE e FROM `ItemStockEntries` e
                WHERE e.`Notes` = 'openingBalance'
                  AND e.`MovementType` = 'In'
                  AND e.`IsDeleted` = 0
                  AND e.`Quantity` > 0
                  AND NOT EXISTS (
                    SELECT 1 FROM `ItemWarehouseStocks` s
                    WHERE s.`ItemId` = e.`ItemId`
                      AND s.`WarehouseId` = e.`WarehouseId`
                      AND s.`IsDeleted` = 0
                      AND s.`Quantity` > 0
                  )
                  AND COALESCE((
                    SELECT SUM(s2.`Quantity`)
                    FROM `ItemWarehouseStocks` s2
                    WHERE s2.`ItemId` = e.`ItemId` AND s2.`IsDeleted` = 0
                  ), 0) = 0;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Irreversible data cleanup
        }
    }
}
