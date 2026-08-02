using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using POS.Db;

#nullable disable

namespace POS.Migrations
{
    [DbContext(typeof(DbConfig))]
    [Migration("20260802150000_AddPayrollAndAdvances")]
    public partial class AddPayrollAndAdvances : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                SET @col_exists := (
                  SELECT COUNT(*) FROM information_schema.COLUMNS
                  WHERE TABLE_SCHEMA = DATABASE() AND LOWER(TABLE_NAME) = 'employees' AND LOWER(COLUMN_NAME) = 'isactive');
                SET @sql := IF(@col_exists = 0,
                  'ALTER TABLE `Employees` ADD COLUMN `IsActive` tinyint(1) NOT NULL DEFAULT 1',
                  'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                SET @col_exists := (
                  SELECT COUNT(*) FROM information_schema.COLUMNS
                  WHERE TABLE_SCHEMA = DATABASE() AND LOWER(TABLE_NAME) = 'employees' AND LOWER(COLUMN_NAME) = 'hiredate');
                SET @sql := IF(@col_exists = 0,
                  'ALTER TABLE `Employees` ADD COLUMN `HireDate` datetime(6) NULL',
                  'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                CREATE TABLE IF NOT EXISTS `EmployeeAdvances` (
                    `Id` int NOT NULL AUTO_INCREMENT,
                    `EmployeeId` int NOT NULL,
                    `Amount` decimal(18,2) NOT NULL,
                    `RemainingAmount` decimal(18,2) NOT NULL,
                    `Date` datetime(6) NOT NULL,
                    `Notes` varchar(1000) CHARACTER SET utf8mb4 NULL,
                    `IsClosed` tinyint(1) NOT NULL,
                    `InsertByUserId` int NOT NULL,
                    `InsertDate` datetime(6) NOT NULL,
                    `UpdateDate` datetime(6) NOT NULL,
                    `IsDeleted` tinyint(1) NOT NULL,
                    CONSTRAINT `PK_EmployeeAdvances` PRIMARY KEY (`Id`),
                    KEY `IX_EmployeeAdvances_EmployeeId` (`EmployeeId`),
                    KEY `IX_EmployeeAdvances_InsertByUserId` (`InsertByUserId`),
                    CONSTRAINT `FK_EmployeeAdvances_Employees_EmployeeId` FOREIGN KEY (`EmployeeId`) REFERENCES `Employees` (`Id`) ON DELETE RESTRICT,
                    CONSTRAINT `FK_EmployeeAdvances_Users_InsertByUserId` FOREIGN KEY (`InsertByUserId`) REFERENCES `Users` (`Id`) ON DELETE RESTRICT
                ) CHARACTER SET utf8mb4;

                CREATE TABLE IF NOT EXISTS `SalaryAdjustments` (
                    `Id` int NOT NULL AUTO_INCREMENT,
                    `EmployeeId` int NOT NULL,
                    `Type` int NOT NULL,
                    `Amount` decimal(18,2) NOT NULL,
                    `AbsenceDays` decimal(18,2) NOT NULL,
                    `Date` datetime(6) NOT NULL,
                    `Notes` varchar(1000) CHARACTER SET utf8mb4 NULL,
                    `InsertByUserId` int NOT NULL,
                    `InsertDate` datetime(6) NOT NULL,
                    `UpdateDate` datetime(6) NOT NULL,
                    `IsDeleted` tinyint(1) NOT NULL,
                    CONSTRAINT `PK_SalaryAdjustments` PRIMARY KEY (`Id`),
                    KEY `IX_SalaryAdjustments_EmployeeId` (`EmployeeId`),
                    KEY `IX_SalaryAdjustments_InsertByUserId` (`InsertByUserId`),
                    CONSTRAINT `FK_SalaryAdjustments_Employees_EmployeeId` FOREIGN KEY (`EmployeeId`) REFERENCES `Employees` (`Id`) ON DELETE RESTRICT,
                    CONSTRAINT `FK_SalaryAdjustments_Users_InsertByUserId` FOREIGN KEY (`InsertByUserId`) REFERENCES `Users` (`Id`) ON DELETE RESTRICT
                ) CHARACTER SET utf8mb4;

                CREATE TABLE IF NOT EXISTS `PayrollRuns` (
                    `Id` int NOT NULL AUTO_INCREMENT,
                    `Year` int NOT NULL,
                    `Month` int NOT NULL,
                    `Status` int NOT NULL,
                    `PeriodStart` datetime(6) NOT NULL,
                    `PeriodEnd` datetime(6) NOT NULL,
                    `ApprovedAt` datetime(6) NULL,
                    `PaidAt` datetime(6) NULL,
                    `Notes` varchar(1000) CHARACTER SET utf8mb4 NULL,
                    `InsertByUserId` int NOT NULL,
                    `InsertDate` datetime(6) NOT NULL,
                    `UpdateDate` datetime(6) NOT NULL,
                    `IsDeleted` tinyint(1) NOT NULL,
                    CONSTRAINT `PK_PayrollRuns` PRIMARY KEY (`Id`),
                    KEY `IX_PayrollRuns_InsertByUserId` (`InsertByUserId`),
                    KEY `IX_PayrollRuns_Year_Month` (`Year`, `Month`),
                    CONSTRAINT `FK_PayrollRuns_Users_InsertByUserId` FOREIGN KEY (`InsertByUserId`) REFERENCES `Users` (`Id`) ON DELETE RESTRICT
                ) CHARACTER SET utf8mb4;

                CREATE TABLE IF NOT EXISTS `PayrollLines` (
                    `Id` int NOT NULL AUTO_INCREMENT,
                    `PayrollRunId` int NOT NULL,
                    `EmployeeId` int NOT NULL,
                    `BaseSalarySnapshot` decimal(18,2) NOT NULL,
                    `SalaryTypeSnapshot` int NOT NULL,
                    `WorkDays` decimal(18,2) NOT NULL,
                    `BaseAmount` decimal(18,2) NOT NULL,
                    `OvertimeAmount` decimal(18,2) NOT NULL,
                    `DeductionAmount` decimal(18,2) NOT NULL,
                    `AbsenceAmount` decimal(18,2) NOT NULL,
                    `AdvanceDeducted` decimal(18,2) NOT NULL,
                    `NetAmount` decimal(18,2) NOT NULL,
                    `Notes` varchar(1000) CHARACTER SET utf8mb4 NULL,
                    `LinkedExpenseId` int NULL,
                    `InsertDate` datetime(6) NOT NULL,
                    `UpdateDate` datetime(6) NOT NULL,
                    `IsDeleted` tinyint(1) NOT NULL,
                    CONSTRAINT `PK_PayrollLines` PRIMARY KEY (`Id`),
                    KEY `IX_PayrollLines_PayrollRunId` (`PayrollRunId`),
                    KEY `IX_PayrollLines_EmployeeId` (`EmployeeId`),
                    CONSTRAINT `FK_PayrollLines_PayrollRuns_PayrollRunId` FOREIGN KEY (`PayrollRunId`) REFERENCES `PayrollRuns` (`Id`) ON DELETE CASCADE,
                    CONSTRAINT `FK_PayrollLines_Employees_EmployeeId` FOREIGN KEY (`EmployeeId`) REFERENCES `Employees` (`Id`) ON DELETE RESTRICT
                ) CHARACTER SET utf8mb4;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TABLE IF EXISTS `PayrollLines`;
                DROP TABLE IF EXISTS `PayrollRuns`;
                DROP TABLE IF EXISTS `SalaryAdjustments`;
                DROP TABLE IF EXISTS `EmployeeAdvances`;
                """);
        }
    }
}
