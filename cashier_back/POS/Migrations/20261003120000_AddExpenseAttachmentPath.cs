using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using POS.Db;

#nullable disable

namespace POS.Migrations
{
    [DbContext(typeof(DbConfig))]
    [Migration("20261003120000_AddExpenseAttachmentPath")]
    public partial class AddExpenseAttachmentPath : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                SET @col_exists := (
                  SELECT COUNT(*) FROM information_schema.COLUMNS
                  WHERE TABLE_SCHEMA = DATABASE()
                    AND LOWER(TABLE_NAME) = 'expenses'
                    AND LOWER(COLUMN_NAME) = 'attachmentpath');
                SET @sql := IF(@col_exists = 0,
                  'ALTER TABLE `Expenses` ADD COLUMN `AttachmentPath` varchar(500) NULL',
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
                    AND LOWER(TABLE_NAME) = 'expenses'
                    AND LOWER(COLUMN_NAME) = 'attachmentpath');
                SET @sql := IF(@col_exists > 0,
                  'ALTER TABLE `Expenses` DROP COLUMN `AttachmentPath`',
                  'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
                """);
        }
    }
}
