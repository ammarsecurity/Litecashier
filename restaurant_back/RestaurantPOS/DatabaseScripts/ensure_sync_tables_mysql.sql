-- جداول المزامنة المحلية (Migration: 20260620120000_AddSyncTables)
-- طبّق على قاعدة POS المحلية فقط — لا تُرفع هذه الجداول للسحابة

CREATE TABLE IF NOT EXISTS `SyncRuns` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `CommercialUserId` int NOT NULL,
  `StartedAt` datetime(6) NOT NULL,
  `FinishedAt` datetime(6) NULL,
  `Status` varchar(20) NOT NULL,
  `Trigger` varchar(20) NOT NULL,
  `RecordsPushed` int NOT NULL DEFAULT 0,
  `FilesPushed` int NOT NULL DEFAULT 0,
  `ErrorMessage` varchar(2000) NULL,
  `InsertDate` datetime(6) NOT NULL,
  `UpdateDate` datetime(6) NOT NULL,
  `IsDeleted` tinyint(1) NOT NULL DEFAULT 0,
  PRIMARY KEY (`Id`),
  KEY `IX_SyncRuns_CommercialUserId_StartedAt` (`CommercialUserId`, `StartedAt`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `SyncWatermarks` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `CommercialUserId` int NOT NULL,
  `TableName` varchar(128) NOT NULL,
  `LastSyncedUpdateDate` datetime(6) NOT NULL,
  `InsertDate` datetime(6) NOT NULL,
  `UpdateDate` datetime(6) NOT NULL,
  `IsDeleted` tinyint(1) NOT NULL DEFAULT 0,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_SyncWatermarks_CommercialUserId_TableName` (`CommercialUserId`, `TableName`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `SyncFileWatermarks` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `CommercialUserId` int NOT NULL,
  `RelativePath` varchar(500) NOT NULL,
  `LastModifiedUtc` datetime(6) NOT NULL,
  `SyncedAt` datetime(6) NOT NULL,
  `InsertDate` datetime(6) NOT NULL,
  `UpdateDate` datetime(6) NOT NULL,
  `IsDeleted` tinyint(1) NOT NULL DEFAULT 0,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_SyncFileWatermarks_CommercialUserId_RelativePath` (`CommercialUserId`, `RelativePath`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `SyncSettings` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `CommercialUserId` int NOT NULL,
  `AutoSyncEnabled` tinyint(1) NOT NULL DEFAULT 0,
  `IntervalMinutes` int NOT NULL DEFAULT 10,
  `InsertDate` datetime(6) NOT NULL,
  `UpdateDate` datetime(6) NOT NULL,
  `IsDeleted` tinyint(1) NOT NULL DEFAULT 0,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_SyncSettings_CommercialUserId` (`CommercialUserId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260620120000_AddSyncTables', '8.0.2');

-- أعمدة ZIP للنسخ الاحتياطي (Migration: 20260620140000_AddSyncRunArchiveFields)
SET @col_exists := (
  SELECT COUNT(*) FROM information_schema.COLUMNS
  WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'SyncRuns' AND COLUMN_NAME = 'ArchiveFileName');
SET @sql := IF(@col_exists = 0,
  'ALTER TABLE `SyncRuns` ADD COLUMN `ArchiveFileName` varchar(260) NULL',
  'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @col_exists := (
  SELECT COUNT(*) FROM information_schema.COLUMNS
  WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'SyncRuns' AND COLUMN_NAME = 'ArchiveSizeBytes');
SET @sql := IF(@col_exists = 0,
  'ALTER TABLE `SyncRuns` ADD COLUMN `ArchiveSizeBytes` bigint NOT NULL DEFAULT 0',
  'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260620140000_AddSyncRunArchiveFields', '8.0.2');
