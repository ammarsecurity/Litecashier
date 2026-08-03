using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RestaurantPOS.Db;

#nullable disable

namespace RestaurantPOS.Migrations
{
    [DbContext(typeof(DbConfig))]
    [Migration("20260803170000_AddPayrollLineIsPaid")]
    public partial class AddPayrollLineIsPaid : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            MigrationBootstrapSql.EnsureTinyIntBoolColumnIfNotExists(
                migrationBuilder,
                "PayrollLines",
                "IsPaid");

            migrationBuilder.Sql(
                """
                SET @col_exists := (
                  SELECT COUNT(*) FROM information_schema.COLUMNS
                  WHERE TABLE_SCHEMA = DATABASE()
                    AND LOWER(TABLE_NAME) = 'payrolllines'
                    AND LOWER(COLUMN_NAME) = 'paidat');
                SET @sql := IF(@col_exists = 0,
                  'ALTER TABLE `PayrollLines` ADD COLUMN `PaidAt` datetime(6) NULL',
                  'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
                """);

            // دورات مصروفة سابقاً: علّم كل الأسطر كمصروفة
            migrationBuilder.Sql(
                """
                UPDATE `PayrollLines` pl
                INNER JOIN `PayrollRuns` pr ON pr.`Id` = pl.`PayrollRunId`
                SET pl.`IsPaid` = 1,
                    pl.`PaidAt` = COALESCE(pl.`PaidAt`, pr.`PaidAt`, UTC_TIMESTAMP(6))
                WHERE pr.`Status` = 2
                  AND pl.`IsDeleted` = 0
                  AND pl.`IsPaid` = 0;
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
                    AND LOWER(COLUMN_NAME) = 'paidat');
                SET @sql := IF(@col_exists > 0,
                  'ALTER TABLE `PayrollLines` DROP COLUMN `PaidAt`',
                  'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                SET @col_exists := (
                  SELECT COUNT(*) FROM information_schema.COLUMNS
                  WHERE TABLE_SCHEMA = DATABASE()
                    AND LOWER(TABLE_NAME) = 'payrolllines'
                    AND LOWER(COLUMN_NAME) = 'ispaid');
                SET @sql := IF(@col_exists > 0,
                  'ALTER TABLE `PayrollLines` DROP COLUMN `IsPaid`',
                  'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
                """);
        }
    }
}
