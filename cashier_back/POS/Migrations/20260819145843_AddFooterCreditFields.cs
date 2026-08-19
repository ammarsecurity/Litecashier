using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS.Migrations
{
    /// <inheritdoc />
    public partial class AddFooterCreditFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                SET @col_exists := (
                  SELECT COUNT(*) FROM information_schema.COLUMNS
                  WHERE TABLE_SCHEMA = DATABASE()
                    AND LOWER(TABLE_NAME) = 'users'
                    AND LOWER(COLUMN_NAME) = 'footercredittext');
                SET @sql := IF(@col_exists = 0,
                  'ALTER TABLE `Users` ADD COLUMN `FooterCreditText` varchar(200) CHARACTER SET utf8mb4 NULL',
                  'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                SET @col_exists := (
                  SELECT COUNT(*) FROM information_schema.COLUMNS
                  WHERE TABLE_SCHEMA = DATABASE()
                    AND LOWER(TABLE_NAME) = 'users'
                    AND LOWER(COLUMN_NAME) = 'footercreditphone');
                SET @sql := IF(@col_exists = 0,
                  'ALTER TABLE `Users` ADD COLUMN `FooterCreditPhone` varchar(30) CHARACTER SET utf8mb4 NULL',
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
                    AND LOWER(TABLE_NAME) = 'users'
                    AND LOWER(COLUMN_NAME) = 'footercreditphone');
                SET @sql := IF(@col_exists > 0,
                  'ALTER TABLE `Users` DROP COLUMN `FooterCreditPhone`',
                  'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                SET @col_exists := (
                  SELECT COUNT(*) FROM information_schema.COLUMNS
                  WHERE TABLE_SCHEMA = DATABASE()
                    AND LOWER(TABLE_NAME) = 'users'
                    AND LOWER(COLUMN_NAME) = 'footercredittext');
                SET @sql := IF(@col_exists > 0,
                  'ALTER TABLE `Users` DROP COLUMN `FooterCreditText`',
                  'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
                """);
        }
    }
}
