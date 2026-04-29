-- Apply Employees table and Expense.EmployeeId (run once on database restaurant_pos)
-- Run from MySQL client:  mysql -h HOST -u USER -p restaurant_pos < Apply_Employees_And_ExpenseEmployeeId.sql

-- 1) Create Employees table
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
    CONSTRAINT `FK_Employees_Users_InsertByUserId` FOREIGN KEY (`InsertByUserId`) REFERENCES `Users` (`Id`),
    CONSTRAINT `FK_Employees_Tags_TagId` FOREIGN KEY (`TagId`) REFERENCES `Tags` (`Id`) ON DELETE SET NULL
) CHARACTER SET utf8mb4;

CREATE INDEX `IX_Employees_InsertByUserId` ON `Employees` (`InsertByUserId`);
CREATE INDEX `IX_Employees_TagId` ON `Employees` (`TagId`);

-- 2) Add EmployeeId to Expenses (run once; if column exists, skip the three lines below)
ALTER TABLE `Expenses` ADD COLUMN `EmployeeId` int NULL;
CREATE INDEX `IX_Expenses_EmployeeId` ON `Expenses` (`EmployeeId`);
ALTER TABLE `Expenses` ADD CONSTRAINT `FK_Expenses_Employees_EmployeeId` FOREIGN KEY (`EmployeeId`) REFERENCES `Employees` (`Id`) ON DELETE SET NULL;

-- 3) Register migrations so EF does not try to apply them again
INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`) VALUES ('20260219000000_AddEmployeesTable', '8.0.2');
INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`) VALUES ('20260219100000_AddEmployeeIdToExpenses', '8.0.2');
