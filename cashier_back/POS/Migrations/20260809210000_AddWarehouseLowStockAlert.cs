using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using POS.Db;

#nullable disable

namespace POS.Migrations
{
    [DbContext(typeof(DbConfig))]
    [Migration("20260809210000_AddWarehouseLowStockAlert")]
    public partial class AddWarehouseLowStockAlert : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                SET @col_exists := (
                  SELECT COUNT(*) FROM information_schema.COLUMNS
                  WHERE TABLE_SCHEMA = DATABASE()
                    AND LOWER(TABLE_NAME) = 'itemwarehousestocks'
                    AND LOWER(COLUMN_NAME) = 'lowstockalertquantity');
                SET @sql := IF(@col_exists = 0,
                  'ALTER TABLE `ItemWarehouseStocks` ADD COLUMN `LowStockAlertQuantity` int NULL',
                  'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                UPDATE `ItemWarehouseStocks` s
                INNER JOIN `Items` i ON i.`Id` = s.`ItemId`
                INNER JOIN `Warehouses` w ON w.`Id` = s.`WarehouseId`
                SET s.`LowStockAlertQuantity` = i.`LowStockAlertQuantity`
                WHERE s.`IsDeleted` = 0
                  AND w.`IsDeleted` = 0
                  AND w.`IsDefault` = 1
                  AND i.`LowStockAlertQuantity` IS NOT NULL
                  AND s.`LowStockAlertQuantity` IS NULL;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE `ItemWarehouseStocks` DROP COLUMN `LowStockAlertQuantity`;
                """);
        }
    }
}
