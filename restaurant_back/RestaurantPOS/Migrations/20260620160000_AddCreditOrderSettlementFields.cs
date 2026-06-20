using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RestaurantPOS.Db;

#nullable disable

namespace RestaurantPOS.Migrations
{
    [DbContext(typeof(DbConfig))]
    [Migration("20260620160000_AddCreditOrderSettlementFields")]
    public partial class AddCreditOrderSettlementFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                SET @col_exists := (
                  SELECT COUNT(*) FROM information_schema.COLUMNS
                  WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'CustomerOrders' AND COLUMN_NAME = 'SettlementPaymentMethod');
                SET @sql := IF(@col_exists = 0,
                  'ALTER TABLE `CustomerOrders` ADD COLUMN `SettlementPaymentMethod` varchar(20) CHARACTER SET utf8mb4 NULL',
                  'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                SET @col_exists := (
                  SELECT COUNT(*) FROM information_schema.COLUMNS
                  WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'CustomerOrders' AND COLUMN_NAME = 'SettledAt');
                SET @sql := IF(@col_exists = 0,
                  'ALTER TABLE `CustomerOrders` ADD COLUMN `SettledAt` datetime(6) NULL',
                  'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                SET @col_exists := (
                  SELECT COUNT(*) FROM information_schema.COLUMNS
                  WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'CustomerOrders' AND COLUMN_NAME = 'SettlementPaymentMethod');
                SET @sql := IF(@col_exists > 0,
                  'ALTER TABLE `CustomerOrders` DROP COLUMN `SettlementPaymentMethod`',
                  'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                SET @col_exists := (
                  SELECT COUNT(*) FROM information_schema.COLUMNS
                  WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'CustomerOrders' AND COLUMN_NAME = 'SettledAt');
                SET @sql := IF(@col_exists > 0,
                  'ALTER TABLE `CustomerOrders` DROP COLUMN `SettledAt`',
                  'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
                """);
        }
    }
}
