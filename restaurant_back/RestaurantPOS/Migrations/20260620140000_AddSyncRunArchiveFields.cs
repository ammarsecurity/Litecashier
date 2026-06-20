using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RestaurantPOS.Db;

#nullable disable

namespace RestaurantPOS.Migrations
{
    [DbContext(typeof(DbConfig))]
    [Migration("20260620140000_AddSyncRunArchiveFields")]
    public partial class AddSyncRunArchiveFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                SET @col_exists := (
                  SELECT COUNT(*) FROM information_schema.COLUMNS
                  WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'SyncRuns' AND COLUMN_NAME = 'ArchiveFileName');
                SET @sql := IF(@col_exists = 0,
                  'ALTER TABLE `SyncRuns` ADD COLUMN `ArchiveFileName` varchar(260) CHARACTER SET utf8mb4 NULL',
                  'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                SET @col_exists := (
                  SELECT COUNT(*) FROM information_schema.COLUMNS
                  WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'SyncRuns' AND COLUMN_NAME = 'ArchiveSizeBytes');
                SET @sql := IF(@col_exists = 0,
                  'ALTER TABLE `SyncRuns` ADD COLUMN `ArchiveSizeBytes` bigint NOT NULL DEFAULT 0',
                  'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                SET @col_exists := (
                  SELECT COUNT(*) FROM information_schema.COLUMNS
                  WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'SyncRuns' AND COLUMN_NAME = 'ArchiveFileName');
                SET @sql := IF(@col_exists > 0,
                  'ALTER TABLE `SyncRuns` DROP COLUMN `ArchiveFileName`',
                  'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                SET @col_exists := (
                  SELECT COUNT(*) FROM information_schema.COLUMNS
                  WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'SyncRuns' AND COLUMN_NAME = 'ArchiveSizeBytes');
                SET @sql := IF(@col_exists > 0,
                  'ALTER TABLE `SyncRuns` DROP COLUMN `ArchiveSizeBytes`',
                  'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
                """);
        }
    }
}
