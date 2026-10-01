using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using POS.Db;

#nullable disable

namespace POS.Migrations
{
    [DbContext(typeof(DbConfig))]
    [Migration("20261001170000_AddUserDomain")]
    public partial class AddUserDomain : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                SET @col_exists := (
                  SELECT COUNT(*) FROM information_schema.COLUMNS
                  WHERE TABLE_SCHEMA = DATABASE()
                    AND LOWER(TABLE_NAME) = 'users'
                    AND LOWER(COLUMN_NAME) = 'domain');
                SET @sql := IF(@col_exists = 0,
                  'ALTER TABLE `Users` ADD COLUMN `Domain` varchar(255) NULL',
                  'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                SET @idx_exists := (
                  SELECT COUNT(*) FROM information_schema.STATISTICS
                  WHERE TABLE_SCHEMA = DATABASE()
                    AND LOWER(TABLE_NAME) = 'users'
                    AND LOWER(INDEX_NAME) = 'ix_users_domain');
                SET @sql2 := IF(@idx_exists = 0,
                  'CREATE UNIQUE INDEX `IX_Users_Domain` ON `Users` (`Domain`)',
                  'SELECT 1');
                PREPARE stmt2 FROM @sql2; EXECUTE stmt2; DEALLOCATE PREPARE stmt2;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP INDEX `IX_Users_Domain` ON `Users`;
                ALTER TABLE `Users` DROP COLUMN `Domain`;
                """);
        }
    }
}
