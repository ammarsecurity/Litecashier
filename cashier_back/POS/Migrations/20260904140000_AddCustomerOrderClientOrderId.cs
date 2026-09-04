using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using POS.Db;

#nullable disable

namespace POS.Migrations
{
    [DbContext(typeof(DbConfig))]
    [Migration("20260904140000_AddCustomerOrderClientOrderId")]
    public partial class AddCustomerOrderClientOrderId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                SET @col := (
                  SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
                  WHERE TABLE_SCHEMA = DATABASE()
                    AND TABLE_NAME = 'CustomerOrders'
                    AND COLUMN_NAME = 'ClientOrderId'
                );
                SET @sql := IF(@col = 0,
                  'ALTER TABLE `CustomerOrders` ADD COLUMN `ClientOrderId` char(36) NULL',
                  'SELECT 1');
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;

                SET @idx := (
                  SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS
                  WHERE TABLE_SCHEMA = DATABASE()
                    AND TABLE_NAME = 'CustomerOrders'
                    AND INDEX_NAME = 'IX_CustomerOrders_ClientOrderId'
                );
                SET @sql := IF(@idx = 0,
                  'CREATE UNIQUE INDEX `IX_CustomerOrders_ClientOrderId` ON `CustomerOrders` (`ClientOrderId`)',
                  'SELECT 1');
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                SET @idx := (
                  SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS
                  WHERE TABLE_SCHEMA = DATABASE()
                    AND TABLE_NAME = 'CustomerOrders'
                    AND INDEX_NAME = 'IX_CustomerOrders_ClientOrderId'
                );
                SET @sql := IF(@idx > 0,
                  'DROP INDEX `IX_CustomerOrders_ClientOrderId` ON `CustomerOrders`',
                  'SELECT 1');
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;

                SET @col := (
                  SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
                  WHERE TABLE_SCHEMA = DATABASE()
                    AND TABLE_NAME = 'CustomerOrders'
                    AND COLUMN_NAME = 'ClientOrderId'
                );
                SET @sql := IF(@col > 0,
                  'ALTER TABLE `CustomerOrders` DROP COLUMN `ClientOrderId`',
                  'SELECT 1');
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
                """);
        }
    }
}
