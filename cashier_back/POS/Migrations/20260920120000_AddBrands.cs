using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using POS.Db;

#nullable disable

namespace POS.Migrations
{
    [DbContext(typeof(DbConfig))]
    [Migration("20260920120000_AddBrands")]
    public partial class AddBrands : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS `Brands` (
                  `Id` int NOT NULL AUTO_INCREMENT,
                  `Name` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
                  `InsertByUserId` int NOT NULL,
                  `InsertDate` datetime(6) NOT NULL,
                  `UpdateDate` datetime(6) NOT NULL,
                  `IsDeleted` tinyint(1) NOT NULL DEFAULT 0,
                  PRIMARY KEY (`Id`),
                  KEY `IX_Brands_InsertByUserId` (`InsertByUserId`),
                  CONSTRAINT `FK_Brands_Users_InsertByUserId` FOREIGN KEY (`InsertByUserId`) REFERENCES `Users` (`Id`) ON DELETE RESTRICT
                ) CHARACTER SET=utf8mb4;

                SET @col_exists := (
                  SELECT COUNT(*) FROM information_schema.COLUMNS
                  WHERE TABLE_SCHEMA = DATABASE()
                    AND LOWER(TABLE_NAME) = 'items'
                    AND LOWER(COLUMN_NAME) = 'brandid');
                SET @sql := IF(@col_exists = 0,
                  'ALTER TABLE `Items` ADD COLUMN `BrandId` int NULL',
                  'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                SET @fk_exists := (
                  SELECT COUNT(*) FROM information_schema.TABLE_CONSTRAINTS
                  WHERE TABLE_SCHEMA = DATABASE()
                    AND TABLE_NAME = 'Items'
                    AND CONSTRAINT_NAME = 'FK_Items_Brands_BrandId'
                    AND CONSTRAINT_TYPE = 'FOREIGN KEY');
                SET @sql := IF(@fk_exists = 0,
                  'ALTER TABLE `Items` ADD CONSTRAINT `FK_Items_Brands_BrandId` FOREIGN KEY (`BrandId`) REFERENCES `Brands` (`Id`) ON DELETE SET NULL',
                  'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                SET @idx_exists := (
                  SELECT COUNT(*) FROM information_schema.STATISTICS
                  WHERE TABLE_SCHEMA = DATABASE()
                    AND TABLE_NAME = 'Items'
                    AND INDEX_NAME = 'IX_Items_BrandId');
                SET @sql := IF(@idx_exists = 0,
                  'CREATE INDEX `IX_Items_BrandId` ON `Items` (`BrandId`)',
                  'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                SET @fk_exists := (
                  SELECT COUNT(*) FROM information_schema.TABLE_CONSTRAINTS
                  WHERE TABLE_SCHEMA = DATABASE()
                    AND TABLE_NAME = 'Items'
                    AND CONSTRAINT_NAME = 'FK_Items_Brands_BrandId');
                SET @sql := IF(@fk_exists > 0,
                  'ALTER TABLE `Items` DROP FOREIGN KEY `FK_Items_Brands_BrandId`',
                  'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                SET @col_exists := (
                  SELECT COUNT(*) FROM information_schema.COLUMNS
                  WHERE TABLE_SCHEMA = DATABASE()
                    AND LOWER(TABLE_NAME) = 'items'
                    AND LOWER(COLUMN_NAME) = 'brandid');
                SET @sql := IF(@col_exists > 0,
                  'ALTER TABLE `Items` DROP COLUMN `BrandId`',
                  'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                DROP TABLE IF EXISTS `Brands`;
                """);
        }
    }
}
