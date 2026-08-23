using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using POS.Db;

#nullable disable

namespace POS.Migrations
{
    [DbContext(typeof(DbConfig))]
    [Migration("20260823160000_AddItemIsNonInventory")]
    public partial class AddItemIsNonInventory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                SET @col_exists := (
                  SELECT COUNT(*) FROM information_schema.COLUMNS
                  WHERE TABLE_SCHEMA = DATABASE()
                    AND LOWER(TABLE_NAME) = 'items'
                    AND LOWER(COLUMN_NAME) = 'isnoninventory');
                SET @sql := IF(@col_exists = 0,
                  'ALTER TABLE `Items` ADD COLUMN `IsNonInventory` tinyint(1) NOT NULL DEFAULT 0',
                  'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE `Items` DROP COLUMN `IsNonInventory`;
                """);
        }
    }
}
