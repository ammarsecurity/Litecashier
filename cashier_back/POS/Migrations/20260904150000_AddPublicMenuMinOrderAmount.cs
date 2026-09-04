using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using POS.Db;

#nullable disable

namespace POS.Migrations
{
    [DbContext(typeof(DbConfig))]
    [Migration("20260904150000_AddPublicMenuMinOrderAmount")]
    public partial class AddPublicMenuMinOrderAmount : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                SET @col := (
                  SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
                  WHERE TABLE_SCHEMA = DATABASE()
                    AND TABLE_NAME = 'Users'
                    AND COLUMN_NAME = 'PublicMenuMinOrderAmount'
                );
                SET @sql := IF(@col = 0,
                  'ALTER TABLE `Users` ADD COLUMN `PublicMenuMinOrderAmount` decimal(18,2) NOT NULL DEFAULT 0',
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
                SET @col := (
                  SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
                  WHERE TABLE_SCHEMA = DATABASE()
                    AND TABLE_NAME = 'Users'
                    AND COLUMN_NAME = 'PublicMenuMinOrderAmount'
                );
                SET @sql := IF(@col > 0,
                  'ALTER TABLE `Users` DROP COLUMN `PublicMenuMinOrderAmount`',
                  'SELECT 1');
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
                """);
        }
    }
}
