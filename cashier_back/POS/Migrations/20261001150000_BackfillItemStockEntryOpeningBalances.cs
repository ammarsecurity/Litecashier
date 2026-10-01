using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using POS.Db;

#nullable disable

namespace POS.Migrations
{
    /// <summary>
    /// One-time: seed ItemStockEntries from current warehouse stock + item prices
    /// so existing inventory appears in the per-item ledger as opening In rows.
    /// Skips items that already have any ledger row.
    /// </summary>
    [DbContext(typeof(DbConfig))]
    [Migration("20261001150000_BackfillItemStockEntryOpeningBalances")]
    public partial class BackfillItemStockEntryOpeningBalances : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                INSERT INTO `ItemStockEntries` (
                  `ItemId`, `WarehouseId`, `MovementType`, `Quantity`,
                  `SellingPrice`, `PurchasingPrice`, `WholesalePrice`, `DisCountPrice`,
                  `BatchPurchasingPrice`, `Notes`, `InsertByUserId`,
                  `InsertDate`, `UpdateDate`, `IsDeleted`
                )
                SELECT
                  s.`ItemId`,
                  s.`WarehouseId`,
                  'In',
                  s.`Quantity`,
                  i.`SellingPrice`,
                  i.`PurchasingPrice`,
                  i.`WholesalePrice`,
                  i.`DisCountPrice`,
                  i.`PurchasingPrice`,
                  'openingBalance',
                  i.`InsertByUserId`,
                  UTC_TIMESTAMP(6),
                  UTC_TIMESTAMP(6),
                  0
                FROM `ItemWarehouseStocks` s
                INNER JOIN `Items` i ON i.`Id` = s.`ItemId` AND i.`IsDeleted` = 0
                WHERE s.`IsDeleted` = 0
                  AND s.`Quantity` > 0
                  AND (i.`IsNonInventory` = 0 OR i.`IsNonInventory` IS NULL)
                  AND NOT EXISTS (
                    SELECT 1 FROM `ItemStockEntries` e
                    WHERE e.`ItemId` = s.`ItemId` AND e.`IsDeleted` = 0
                  );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM `ItemStockEntries`
                WHERE `Notes` = 'openingBalance'
                  AND `MovementType` = 'In';
                """);
        }
    }
}
