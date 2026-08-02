using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantPOS.Migrations
{
    /// <inheritdoc />
    public partial class AddIsPublicOrderPrinterToPrinter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent: بعض قواعد البيانات قد تكون طبّقت سجل الترحيل دون العمود
            MigrationBootstrapSql.EnsureTinyIntBoolColumnIfNotExists(
                migrationBuilder,
                "Printers",
                "IsPublicOrderPrinter");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                SET @col_exists := (
                  SELECT COUNT(*) FROM information_schema.COLUMNS
                  WHERE TABLE_SCHEMA = DATABASE()
                    AND LOWER(TABLE_NAME) = 'printers'
                    AND LOWER(COLUMN_NAME) = 'ispublicorderprinter');
                SET @sql := IF(@col_exists > 0,
                  'ALTER TABLE `Printers` DROP COLUMN `IsPublicOrderPrinter`',
                  'SELECT 1');
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
                """);
        }
    }
}
