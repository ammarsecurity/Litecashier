using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using POS.Db;

#nullable disable

namespace POS.Migrations
{
    [DbContext(typeof(DbConfig))]
    [Migration("20260913170000_AddPosLayout")]
    public partial class AddPosLayout : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                SET @col_exists := (
                  SELECT COUNT(*) FROM information_schema.COLUMNS
                  WHERE TABLE_SCHEMA = DATABASE()
                    AND LOWER(TABLE_NAME) = 'users'
                    AND LOWER(COLUMN_NAME) = 'poslayout');
                SET @sql := IF(@col_exists = 0,
                  'ALTER TABLE `Users` ADD COLUMN `PosLayout` varchar(20) NOT NULL DEFAULT ''Classic''',
                  'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE `Users` DROP COLUMN `PosLayout`;
                """);
        }
    }
}
