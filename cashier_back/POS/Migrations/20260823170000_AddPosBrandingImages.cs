using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using POS.Db;

#nullable disable

namespace POS.Migrations
{
    [DbContext(typeof(DbConfig))]
    [Migration("20260823170000_AddPosBrandingImages")]
    public partial class AddPosBrandingImages : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                SET @col := (SELECT COUNT(*) FROM information_schema.COLUMNS
                  WHERE TABLE_SCHEMA = DATABASE() AND LOWER(TABLE_NAME) = 'users' AND LOWER(COLUMN_NAME) = 'cartwatermarklogo');
                SET @sql := IF(@col = 0, 'ALTER TABLE `Users` ADD COLUMN `CartWatermarkLogo` longtext NULL', 'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                SET @col := (SELECT COUNT(*) FROM information_schema.COLUMNS
                  WHERE TABLE_SCHEMA = DATABASE() AND LOWER(TABLE_NAME) = 'users' AND LOWER(COLUMN_NAME) = 'cartwatermarkopacity');
                SET @sql := IF(@col = 0, 'ALTER TABLE `Users` ADD COLUMN `CartWatermarkOpacity` int NOT NULL DEFAULT 18', 'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                SET @col := (SELECT COUNT(*) FROM information_schema.COLUMNS
                  WHERE TABLE_SCHEMA = DATABASE() AND LOWER(TABLE_NAME) = 'users' AND LOWER(COLUMN_NAME) = 'defaultproductimage');
                SET @sql := IF(@col = 0, 'ALTER TABLE `Users` ADD COLUMN `DefaultProductImage` longtext NULL', 'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE `Users` DROP COLUMN `CartWatermarkLogo`;
                ALTER TABLE `Users` DROP COLUMN `CartWatermarkOpacity`;
                ALTER TABLE `Users` DROP COLUMN `DefaultProductImage`;
                """);
        }
    }
}
