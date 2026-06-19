using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantPOS.Migrations
{
    /// <inheritdoc />
    public partial class AddSyncTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS `SyncRuns` (
                    `Id` int NOT NULL AUTO_INCREMENT,
                    `CommercialUserId` int NOT NULL,
                    `StartedAt` datetime(6) NOT NULL,
                    `FinishedAt` datetime(6) NULL,
                    `Status` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
                    `Trigger` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
                    `RecordsPushed` int NOT NULL,
                    `FilesPushed` int NOT NULL,
                    `ErrorMessage` varchar(2000) CHARACTER SET utf8mb4 NULL,
                    `InsertDate` datetime(6) NOT NULL,
                    `UpdateDate` datetime(6) NOT NULL,
                    `IsDeleted` tinyint(1) NOT NULL,
                    CONSTRAINT `PK_SyncRuns` PRIMARY KEY (`Id`)
                ) CHARACTER SET utf8mb4;

                CREATE TABLE IF NOT EXISTS `SyncWatermarks` (
                    `Id` int NOT NULL AUTO_INCREMENT,
                    `CommercialUserId` int NOT NULL,
                    `TableName` varchar(128) CHARACTER SET utf8mb4 NOT NULL,
                    `LastSyncedUpdateDate` datetime(6) NOT NULL,
                    `InsertDate` datetime(6) NOT NULL,
                    `UpdateDate` datetime(6) NOT NULL,
                    `IsDeleted` tinyint(1) NOT NULL,
                    CONSTRAINT `PK_SyncWatermarks` PRIMARY KEY (`Id`)
                ) CHARACTER SET utf8mb4;

                CREATE TABLE IF NOT EXISTS `SyncFileWatermarks` (
                    `Id` int NOT NULL AUTO_INCREMENT,
                    `CommercialUserId` int NOT NULL,
                    `RelativePath` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
                    `LastModifiedUtc` datetime(6) NOT NULL,
                    `SyncedAt` datetime(6) NOT NULL,
                    `InsertDate` datetime(6) NOT NULL,
                    `UpdateDate` datetime(6) NOT NULL,
                    `IsDeleted` tinyint(1) NOT NULL,
                    CONSTRAINT `PK_SyncFileWatermarks` PRIMARY KEY (`Id`)
                ) CHARACTER SET utf8mb4;

                CREATE TABLE IF NOT EXISTS `SyncSettings` (
                    `Id` int NOT NULL AUTO_INCREMENT,
                    `CommercialUserId` int NOT NULL,
                    `AutoSyncEnabled` tinyint(1) NOT NULL,
                    `IntervalMinutes` int NOT NULL,
                    `InsertDate` datetime(6) NOT NULL,
                    `UpdateDate` datetime(6) NOT NULL,
                    `IsDeleted` tinyint(1) NOT NULL,
                    CONSTRAINT `PK_SyncSettings` PRIMARY KEY (`Id`)
                ) CHARACTER SET utf8mb4;
                """);

            migrationBuilder.Sql(
                """
                SET @idx_exists := (
                  SELECT COUNT(*) FROM information_schema.STATISTICS
                  WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'SyncRuns' AND INDEX_NAME = 'IX_SyncRuns_CommercialUserId_StartedAt');
                SET @sql := IF(@idx_exists = 0,
                  'CREATE INDEX `IX_SyncRuns_CommercialUserId_StartedAt` ON `SyncRuns` (`CommercialUserId`, `StartedAt`)',
                  'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                SET @idx_exists := (
                  SELECT COUNT(*) FROM information_schema.STATISTICS
                  WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'SyncWatermarks' AND INDEX_NAME = 'IX_SyncWatermarks_CommercialUserId_TableName');
                SET @sql := IF(@idx_exists = 0,
                  'CREATE UNIQUE INDEX `IX_SyncWatermarks_CommercialUserId_TableName` ON `SyncWatermarks` (`CommercialUserId`, `TableName`)',
                  'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                SET @idx_exists := (
                  SELECT COUNT(*) FROM information_schema.STATISTICS
                  WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'SyncFileWatermarks' AND INDEX_NAME = 'IX_SyncFileWatermarks_CommercialUserId_RelativePath');
                SET @sql := IF(@idx_exists = 0,
                  'CREATE UNIQUE INDEX `IX_SyncFileWatermarks_CommercialUserId_RelativePath` ON `SyncFileWatermarks` (`CommercialUserId`, `RelativePath`)',
                  'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                SET @idx_exists := (
                  SELECT COUNT(*) FROM information_schema.STATISTICS
                  WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'SyncSettings' AND INDEX_NAME = 'IX_SyncSettings_CommercialUserId');
                SET @sql := IF(@idx_exists = 0,
                  'CREATE UNIQUE INDEX `IX_SyncSettings_CommercialUserId` ON `SyncSettings` (`CommercialUserId`)',
                  'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "SyncFileWatermarks");
            migrationBuilder.DropTable(name: "SyncRuns");
            migrationBuilder.DropTable(name: "SyncSettings");
            migrationBuilder.DropTable(name: "SyncWatermarks");
        }
    }
}
