using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using POS.Db;

#nullable disable

namespace POS.Migrations
{
    [DbContext(typeof(DbConfig))]
    [Migration("20260809180000_AddWarehouses")]
    public partial class AddWarehouses : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS `Warehouses` (
                  `Id` int NOT NULL AUTO_INCREMENT,
                  `Name` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
                  `IsDefault` tinyint(1) NOT NULL DEFAULT 0,
                  `IsActive` tinyint(1) NOT NULL DEFAULT 1,
                  `InsertByUserId` int NOT NULL,
                  `InsertDate` datetime(6) NOT NULL,
                  `UpdateDate` datetime(6) NOT NULL,
                  `IsDeleted` tinyint(1) NOT NULL DEFAULT 0,
                  PRIMARY KEY (`Id`),
                  KEY `IX_Warehouses_InsertByUserId_Name` (`InsertByUserId`, `Name`),
                  CONSTRAINT `FK_Warehouses_Users_InsertByUserId`
                    FOREIGN KEY (`InsertByUserId`) REFERENCES `Users` (`Id`)
                ) CHARACTER SET=utf8mb4;

                CREATE TABLE IF NOT EXISTS `ItemWarehouseStocks` (
                  `Id` int NOT NULL AUTO_INCREMENT,
                  `ItemId` int NOT NULL,
                  `WarehouseId` int NOT NULL,
                  `Quantity` int NOT NULL DEFAULT 0,
                  `InsertDate` datetime(6) NOT NULL,
                  `UpdateDate` datetime(6) NOT NULL,
                  `IsDeleted` tinyint(1) NOT NULL DEFAULT 0,
                  PRIMARY KEY (`Id`),
                  UNIQUE KEY `IX_ItemWarehouseStocks_ItemId_WarehouseId` (`ItemId`, `WarehouseId`),
                  KEY `IX_ItemWarehouseStocks_WarehouseId` (`WarehouseId`),
                  CONSTRAINT `FK_ItemWarehouseStocks_Items_ItemId`
                    FOREIGN KEY (`ItemId`) REFERENCES `Items` (`Id`) ON DELETE CASCADE,
                  CONSTRAINT `FK_ItemWarehouseStocks_Warehouses_WarehouseId`
                    FOREIGN KEY (`WarehouseId`) REFERENCES `Warehouses` (`Id`) ON DELETE CASCADE
                ) CHARACTER SET=utf8mb4;

                SET @col_exists := (
                  SELECT COUNT(*) FROM information_schema.COLUMNS
                  WHERE TABLE_SCHEMA = DATABASE()
                    AND LOWER(TABLE_NAME) = 'customerorders'
                    AND LOWER(COLUMN_NAME) = 'warehouseid');
                SET @sql := IF(@col_exists = 0,
                  'ALTER TABLE `CustomerOrders` ADD COLUMN `WarehouseId` int NULL',
                  'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                SET @fk_exists := (
                  SELECT COUNT(*) FROM information_schema.TABLE_CONSTRAINTS
                  WHERE TABLE_SCHEMA = DATABASE()
                    AND TABLE_NAME = 'CustomerOrders'
                    AND CONSTRAINT_NAME = 'FK_CustomerOrders_Warehouses_WarehouseId');
                SET @sql := IF(@fk_exists = 0,
                  'ALTER TABLE `CustomerOrders` ADD CONSTRAINT `FK_CustomerOrders_Warehouses_WarehouseId` FOREIGN KEY (`WarehouseId`) REFERENCES `Warehouses` (`Id`) ON DELETE SET NULL',
                  'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                SET @col_exists := (
                  SELECT COUNT(*) FROM information_schema.COLUMNS
                  WHERE TABLE_SCHEMA = DATABASE()
                    AND LOWER(TABLE_NAME) = 'catalogstockreturns'
                    AND LOWER(COLUMN_NAME) = 'warehouseid');
                SET @sql := IF(@col_exists = 0,
                  'ALTER TABLE `CatalogStockReturns` ADD COLUMN `WarehouseId` int NULL',
                  'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                SET @fk_exists := (
                  SELECT COUNT(*) FROM information_schema.TABLE_CONSTRAINTS
                  WHERE TABLE_SCHEMA = DATABASE()
                    AND TABLE_NAME = 'CatalogStockReturns'
                    AND CONSTRAINT_NAME = 'FK_CatalogStockReturns_Warehouses_WarehouseId');
                SET @sql := IF(@fk_exists = 0,
                  'ALTER TABLE `CatalogStockReturns` ADD CONSTRAINT `FK_CatalogStockReturns_Warehouses_WarehouseId` FOREIGN KEY (`WarehouseId`) REFERENCES `Warehouses` (`Id`) ON DELETE SET NULL',
                  'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                INSERT INTO `Warehouses` (`Name`, `IsDefault`, `IsActive`, `InsertByUserId`, `InsertDate`, `UpdateDate`, `IsDeleted`)
                SELECT N'المخزن الرئيسي', 1, 1, u.`Id`, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6), 0
                FROM `Users` u
                WHERE u.`IsDeleted` = 0
                  AND u.`Role` = 'Commercial'
                  AND NOT EXISTS (
                    SELECT 1 FROM `Warehouses` w
                    WHERE w.`InsertByUserId` = u.`Id` AND w.`IsDeleted` = 0);

                INSERT INTO `ItemWarehouseStocks` (`ItemId`, `WarehouseId`, `Quantity`, `InsertDate`, `UpdateDate`, `IsDeleted`)
                SELECT i.`Id`, w.`Id`, i.`Quantity`, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6), 0
                FROM `Items` i
                INNER JOIN `Warehouses` w
                  ON w.`InsertByUserId` = i.`InsertByUserId`
                 AND w.`IsDefault` = 1
                 AND w.`IsDeleted` = 0
                WHERE i.`IsDeleted` = 0
                  AND NOT EXISTS (
                    SELECT 1 FROM `ItemWarehouseStocks` s
                    WHERE s.`ItemId` = i.`Id` AND s.`WarehouseId` = w.`Id` AND s.`IsDeleted` = 0);

                UPDATE `CustomerOrders` o
                INNER JOIN `Users` u ON u.`Id` = o.`InsertByUserId`
                INNER JOIN `Warehouses` w ON w.`IsDeleted` = 0 AND w.`IsDefault` = 1
                  AND w.`InsertByUserId` = CASE
                    WHEN u.`Role` = 'Commercial' THEN u.`Id`
                    ELSE u.`InsertByUserId`
                  END
                SET o.`WarehouseId` = w.`Id`
                WHERE o.`WarehouseId` IS NULL AND o.`IsDeleted` = 0;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE `CatalogStockReturns` DROP FOREIGN KEY `FK_CatalogStockReturns_Warehouses_WarehouseId`;
                ALTER TABLE `CustomerOrders` DROP FOREIGN KEY `FK_CustomerOrders_Warehouses_WarehouseId`;
                ALTER TABLE `CatalogStockReturns` DROP COLUMN `WarehouseId`;
                ALTER TABLE `CustomerOrders` DROP COLUMN `WarehouseId`;
                DROP TABLE IF EXISTS `ItemWarehouseStocks`;
                DROP TABLE IF EXISTS `Warehouses`;
                """);
        }
    }
}
