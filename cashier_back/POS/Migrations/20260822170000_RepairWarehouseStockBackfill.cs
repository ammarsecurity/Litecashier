using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using POS.Db;

#nullable disable

namespace POS.Migrations
{
    /// <summary>
    /// One-time repair: after AddWarehouses, some catalogs kept Items.Quantity but
    /// ItemWarehouseStocks stayed empty/zero (POS then shows out-of-stock).
    /// Safe to re-run: only fills missing/zero default-warehouse rows when no warehouse has stock.
    /// </summary>
    [DbContext(typeof(DbConfig))]
    [Migration("20260822170000_RepairWarehouseStockBackfill")]
    public partial class RepairWarehouseStockBackfill : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                INSERT INTO `Warehouses` (`Name`, `IsDefault`, `IsActive`, `InsertByUserId`, `InsertDate`, `UpdateDate`, `IsDeleted`)
                SELECT N'المخزن الرئيسي', 1, 1, u.`Id`, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6), 0
                FROM `Users` u
                WHERE u.`IsDeleted` = 0
                  AND u.`Role` = 'Commercial'
                  AND NOT EXISTS (
                    SELECT 1 FROM `Warehouses` w
                    WHERE w.`InsertByUserId` = u.`Id` AND w.`IsDeleted` = 0);

                INSERT INTO `ItemWarehouseStocks` (`ItemId`, `WarehouseId`, `Quantity`, `InsertDate`, `UpdateDate`, `IsDeleted`)
                SELECT i.`Id`, w.`Id`, i.`Quantity`, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6), 0
                FROM `Items` i
                INNER JOIN `Users` u ON u.`Id` = i.`InsertByUserId` AND u.`IsDeleted` = 0
                INNER JOIN `Warehouses` w
                  ON w.`IsDeleted` = 0
                 AND w.`IsDefault` = 1
                 AND w.`InsertByUserId` = CASE
                   WHEN u.`Role` = 'Commercial' THEN u.`Id`
                   ELSE u.`InsertByUserId`
                 END
                WHERE i.`IsDeleted` = 0
                  AND i.`Quantity` > 0
                  AND NOT EXISTS (
                    SELECT 1 FROM `ItemWarehouseStocks` s
                    WHERE s.`ItemId` = i.`Id`
                      AND s.`WarehouseId` = w.`Id`
                      AND s.`IsDeleted` = 0);

                UPDATE `ItemWarehouseStocks` s
                INNER JOIN `Items` i ON i.`Id` = s.`ItemId` AND i.`IsDeleted` = 0
                INNER JOIN `Warehouses` w ON w.`Id` = s.`WarehouseId` AND w.`IsDeleted` = 0 AND w.`IsDefault` = 1
                SET s.`Quantity` = i.`Quantity`,
                    s.`UpdateDate` = UTC_TIMESTAMP(6)
                WHERE s.`IsDeleted` = 0
                  AND s.`Quantity` = 0
                  AND i.`Quantity` > 0
                  AND NOT EXISTS (
                    SELECT 1 FROM `ItemWarehouseStocks` s2
                    WHERE s2.`ItemId` = i.`Id`
                      AND s2.`IsDeleted` = 0
                      AND s2.`Quantity` > 0);

                UPDATE `Items` i
                SET i.`Quantity` = COALESCE((
                      SELECT SUM(s.`Quantity`)
                      FROM `ItemWarehouseStocks` s
                      WHERE s.`ItemId` = i.`Id` AND s.`IsDeleted` = 0
                    ), 0),
                    i.`UpdateDate` = UTC_TIMESTAMP(6)
                WHERE i.`IsDeleted` = 0
                  AND i.`Quantity` <> COALESCE((
                      SELECT SUM(s.`Quantity`)
                      FROM `ItemWarehouseStocks` s
                      WHERE s.`ItemId` = i.`Id` AND s.`IsDeleted` = 0
                    ), 0);
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Data repair — no schema rollback.
        }
    }
}
