using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using POS.Db;

#nullable disable

namespace POS.Migrations
{
    [DbContext(typeof(DbConfig))]
    [Migration("20260809200000_AddPrintInvoiceFormat")]
    public partial class AddPrintInvoiceFormat : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                SET @col_exists := (
                  SELECT COUNT(*) FROM information_schema.COLUMNS
                  WHERE TABLE_SCHEMA = DATABASE()
                    AND LOWER(TABLE_NAME) = 'users'
                    AND LOWER(COLUMN_NAME) = 'printinvoiceformat');
                SET @sql := IF(@col_exists = 0,
                  'ALTER TABLE `Users` ADD COLUMN `PrintInvoiceFormat` varchar(10) NOT NULL DEFAULT ''Pos''',
                  'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE `Users` DROP COLUMN `PrintInvoiceFormat`;
                """);
        }
    }
}
