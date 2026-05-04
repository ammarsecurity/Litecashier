-- One-time repair: Feb 2026 migrations exist in code but are not in EF's migration chain.
-- Run: mysql -h HOST -u USER -p restaurant_po1s < fix_missing_feb2026_migrations.sql

-- 1) Employees (if missing)
CREATE TABLE IF NOT EXISTS `Employees` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Name` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `PhoneNumber` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
    `Address` varchar(500) CHARACTER SET utf8mb4 NULL,
    `JobTitle` varchar(200) CHARACTER SET utf8mb4 NULL,
    `Salary` decimal(18,2) NOT NULL,
    `SalaryType` int NOT NULL,
    `TagId` int NULL,
    `InsertByUserId` int NOT NULL,
    `InsertDate` datetime(6) NOT NULL,
    `UpdateDate` datetime(6) NOT NULL,
    `IsDeleted` tinyint(1) NOT NULL,
    CONSTRAINT `PK_Employees` PRIMARY KEY (`Id`),
    KEY `IX_Employees_InsertByUserId` (`InsertByUserId`),
    KEY `IX_Employees_TagId` (`TagId`),
    CONSTRAINT `FK_Employees_Users_InsertByUserId` FOREIGN KEY (`InsertByUserId`) REFERENCES `users` (`Id`),
    CONSTRAINT `FK_Employees_Tags_TagId` FOREIGN KEY (`TagId`) REFERENCES `tags` (`Id`) ON DELETE SET NULL
) CHARACTER SET utf8mb4;

-- 2) Expenses.EmployeeId (if missing)
SET @col_exists := (
  SELECT COUNT(*) FROM information_schema.COLUMNS
  WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'expenses' AND COLUMN_NAME = 'EmployeeId');
SET @sql := IF(@col_exists = 0,
  'ALTER TABLE `expenses` ADD COLUMN `EmployeeId` int NULL',
  'SELECT 1');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @idx_exists := (
  SELECT COUNT(*) FROM information_schema.STATISTICS
  WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'expenses' AND INDEX_NAME = 'IX_Expenses_EmployeeId');
SET @sql2 := IF(@idx_exists = 0,
  'CREATE INDEX `IX_Expenses_EmployeeId` ON `expenses` (`EmployeeId`)',
  'SELECT 1');
PREPARE stmt2 FROM @sql2;
EXECUTE stmt2;
DEALLOCATE PREPARE stmt2;

SET @fk_exists := (
  SELECT COUNT(*) FROM information_schema.TABLE_CONSTRAINTS
  WHERE TABLE_SCHEMA = DATABASE() AND CONSTRAINT_NAME = 'FK_Expenses_Employees_EmployeeId');
SET @sql3 := IF(@fk_exists = 0,
  'ALTER TABLE `expenses` ADD CONSTRAINT `FK_Expenses_Employees_EmployeeId` FOREIGN KEY (`EmployeeId`) REFERENCES `Employees` (`Id`) ON DELETE SET NULL',
  'SELECT 1');
PREPARE stmt3 FROM @sql3;
EXECUTE stmt3;
DEALLOCATE PREPARE stmt3;

-- 3) StockMovements
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
    KEY `IX_StockMovements_InsertByUserId` (`InsertByUserId`),
    CONSTRAINT `FK_StockMovements_Users_InsertByUserId` FOREIGN KEY (`InsertByUserId`) REFERENCES `users` (`Id`)
) CHARACTER SET utf8mb4;

-- 4) Register migrations so EF applies only May 2026 migrations next
INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`) VALUES ('20260219000000_AddEmployeesTable', '8.0.2');
INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`) VALUES ('20260219100000_AddEmployeeIdToExpenses', '8.0.2');
INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`) VALUES ('20260219120000_AddInventoryAndStockMovements', '8.0.2');
