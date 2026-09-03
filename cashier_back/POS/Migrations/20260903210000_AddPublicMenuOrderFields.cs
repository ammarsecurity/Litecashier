using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using POS.Db;

#nullable disable

namespace POS.Migrations
{
    [DbContext(typeof(DbConfig))]
    [Migration("20260903210000_AddPublicMenuOrderFields")]
    public partial class AddPublicMenuOrderFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                SET @col := (SELECT COUNT(*) FROM information_schema.COLUMNS
                  WHERE TABLE_SCHEMA = DATABASE() AND LOWER(TABLE_NAME) = 'customerorders' AND LOWER(COLUMN_NAME) = 'ordersource');
                SET @sql := IF(@col = 0, 'ALTER TABLE `CustomerOrders` ADD COLUMN `OrderSource` varchar(20) NOT NULL DEFAULT ''Pos''', 'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                SET @col := (SELECT COUNT(*) FROM information_schema.COLUMNS
                  WHERE TABLE_SCHEMA = DATABASE() AND LOWER(TABLE_NAME) = 'customerorders' AND LOWER(COLUMN_NAME) = 'orderstatus');
                SET @sql := IF(@col = 0, 'ALTER TABLE `CustomerOrders` ADD COLUMN `OrderStatus` varchar(20) NULL', 'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                SET @col := (SELECT COUNT(*) FROM information_schema.COLUMNS
                  WHERE TABLE_SCHEMA = DATABASE() AND LOWER(TABLE_NAME) = 'customerorders' AND LOWER(COLUMN_NAME) = 'customername');
                SET @sql := IF(@col = 0, 'ALTER TABLE `CustomerOrders` ADD COLUMN `CustomerName` varchar(120) NULL', 'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                SET @col := (SELECT COUNT(*) FROM information_schema.COLUMNS
                  WHERE TABLE_SCHEMA = DATABASE() AND LOWER(TABLE_NAME) = 'customerorders' AND LOWER(COLUMN_NAME) = 'customerphone');
                SET @sql := IF(@col = 0, 'ALTER TABLE `CustomerOrders` ADD COLUMN `CustomerPhone` varchar(30) NULL', 'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                SET @col := (SELECT COUNT(*) FROM information_schema.COLUMNS
                  WHERE TABLE_SCHEMA = DATABASE() AND LOWER(TABLE_NAME) = 'customerorders' AND LOWER(COLUMN_NAME) = 'notes');
                SET @sql := IF(@col = 0, 'ALTER TABLE `CustomerOrders` ADD COLUMN `Notes` varchar(1000) NULL', 'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE `CustomerOrders` DROP COLUMN `OrderSource`;
                ALTER TABLE `CustomerOrders` DROP COLUMN `OrderStatus`;
                ALTER TABLE `CustomerOrders` DROP COLUMN `CustomerName`;
                ALTER TABLE `CustomerOrders` DROP COLUMN `CustomerPhone`;
                ALTER TABLE `CustomerOrders` DROP COLUMN `Notes`;
                """);
        }
    }
}
