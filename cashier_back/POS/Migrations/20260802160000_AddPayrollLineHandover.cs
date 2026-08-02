using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using POS.Db;

#nullable disable

namespace POS.Migrations
{
    [DbContext(typeof(DbConfig))]
    [Migration("20260802160000_AddPayrollLineHandover")]
    public partial class AddPayrollLineHandover : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                SET @col_exists := (
                  SELECT COUNT(*) FROM information_schema.COLUMNS
                  WHERE TABLE_SCHEMA = DATABASE()
                    AND LOWER(TABLE_NAME) = 'payrolllines'
                    AND LOWER(COLUMN_NAME) = 'ishandedover');
                SET @sql := IF(@col_exists = 0,
                  'ALTER TABLE `PayrollLines` ADD COLUMN `IsHandedOver` tinyint(1) NOT NULL DEFAULT 0',
                  'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                SET @col_exists := (
                  SELECT COUNT(*) FROM information_schema.COLUMNS
                  WHERE TABLE_SCHEMA = DATABASE()
                    AND LOWER(TABLE_NAME) = 'payrolllines'
                    AND LOWER(COLUMN_NAME) = 'handedoverat');
                SET @sql := IF(@col_exists = 0,
                  'ALTER TABLE `PayrollLines` ADD COLUMN `HandedOverAt` datetime(6) NULL',
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
                  WHERE TABLE_SCHEMA = DATABASE()
                    AND LOWER(TABLE_NAME) = 'payrolllines'
                    AND LOWER(COLUMN_NAME) = 'handedoverat');
                SET @sql := IF(@col_exists > 0,
                  'ALTER TABLE `PayrollLines` DROP COLUMN `HandedOverAt`',
                  'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                SET @col_exists := (
                  SELECT COUNT(*) FROM information_schema.COLUMNS
                  WHERE TABLE_SCHEMA = DATABASE()
                    AND LOWER(TABLE_NAME) = 'payrolllines'
                    AND LOWER(COLUMN_NAME) = 'ishandedover');
                SET @sql := IF(@col_exists > 0,
                  'ALTER TABLE `PayrollLines` DROP COLUMN `IsHandedOver`',
                  'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
                """);
        }
    }
}
