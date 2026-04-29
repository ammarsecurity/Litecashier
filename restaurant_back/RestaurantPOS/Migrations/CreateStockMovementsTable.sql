-- Run this script on your MySQL database (e.g. restaurant_pos) if StockMovements table doesn't exist.
-- You can run it via: mysql -u your_user -p restaurant_pos < CreateStockMovementsTable.sql
-- Or paste and run in MySQL Workbench / phpMyAdmin.

CREATE TABLE IF NOT EXISTS `StockMovements` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `MaterialName` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `MovementType` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
    `Quantity` decimal(18,2) NOT NULL,
    `SupplierName` varchar(200) CHARACTER SET utf8mb4 NULL,
    `Amount` decimal(18,2) NULL,
    `UnitType` varchar(50) CHARACTER SET utf8mb4 NULL,
    `ReceiptAttachmentPath` varchar(500) CHARACTER SET utf8mb4 NULL,
    `Notes` varchar(1000) CHARACTER SET utf8mb4 NULL,
    `InsertByUserId` int NOT NULL,
    `InsertDate` datetime(6) NOT NULL,
    `UpdateDate` datetime(6) NOT NULL,
    `IsDeleted` tinyint(1) NOT NULL,
    CONSTRAINT `PK_StockMovements` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_StockMovements_Users_InsertByUserId` FOREIGN KEY (`InsertByUserId`) REFERENCES `Users` (`Id`) ON DELETE NO ACTION
) CHARACTER SET utf8mb4;

CREATE INDEX `IX_StockMovements_InsertByUserId` ON `StockMovements` (`InsertByUserId`);
