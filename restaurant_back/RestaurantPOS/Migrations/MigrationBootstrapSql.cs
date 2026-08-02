using Microsoft.EntityFrameworkCore.Migrations;

namespace RestaurantPOS.Migrations;

/// <summary>
/// Idempotent schema bootstrap for tables added outside EF's migration chain (Feb 2026 orphans).
/// Used by chained migrations so fresh and partial databases both migrate cleanly.
/// </summary>
internal static class MigrationBootstrapSql
{
    public static void EnsureEmployeesTable(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
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
                CONSTRAINT `FK_Employees_Users_InsertByUserId` FOREIGN KEY (`InsertByUserId`) REFERENCES `Users` (`Id`),
                CONSTRAINT `FK_Employees_Tags_TagId` FOREIGN KEY (`TagId`) REFERENCES `Tags` (`Id`) ON DELETE SET NULL
            ) CHARACTER SET utf8mb4;
            """);
    }

    public static void EnsureExpensesEmployeeId(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            SET @col_exists := (
              SELECT COUNT(*) FROM information_schema.COLUMNS
              WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Expenses' AND COLUMN_NAME = 'EmployeeId');
            SET @sql := IF(@col_exists = 0,
              'ALTER TABLE `Expenses` ADD COLUMN `EmployeeId` int NULL',
              'SELECT 1');
            PREPARE stmt FROM @sql;
            EXECUTE stmt;
            DEALLOCATE PREPARE stmt;

            SET @idx_exists := (
              SELECT COUNT(*) FROM information_schema.STATISTICS
              WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Expenses' AND INDEX_NAME = 'IX_Expenses_EmployeeId');
            SET @sql2 := IF(@idx_exists = 0,
              'CREATE INDEX `IX_Expenses_EmployeeId` ON `Expenses` (`EmployeeId`)',
              'SELECT 1');
            PREPARE stmt2 FROM @sql2;
            EXECUTE stmt2;
            DEALLOCATE PREPARE stmt2;

            SET @fk_exists := (
              SELECT COUNT(*) FROM information_schema.TABLE_CONSTRAINTS
              WHERE TABLE_SCHEMA = DATABASE() AND CONSTRAINT_NAME = 'FK_Expenses_Employees_EmployeeId');
            SET @sql3 := IF(@fk_exists = 0,
              'ALTER TABLE `Expenses` ADD CONSTRAINT `FK_Expenses_Employees_EmployeeId` FOREIGN KEY (`EmployeeId`) REFERENCES `Employees` (`Id`) ON DELETE SET NULL',
              'SELECT 1');
            PREPARE stmt3 FROM @sql3;
            EXECUTE stmt3;
            DEALLOCATE PREPARE stmt3;
            """);
    }

    public static void EnsureStockMovementsTable(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
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
                CONSTRAINT `FK_StockMovements_Users_InsertByUserId` FOREIGN KEY (`InsertByUserId`) REFERENCES `Users` (`Id`)
            ) CHARACTER SET utf8mb4;
            """);
    }

    public static void EnsureSuppliersTable(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TABLE IF NOT EXISTS `Suppliers` (
                `Id` int NOT NULL AUTO_INCREMENT,
                `Name` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
                `Notes` varchar(500) CHARACTER SET utf8mb4 NULL,
                `InsertByUserId` int NOT NULL,
                `InsertDate` datetime(6) NOT NULL,
                `UpdateDate` datetime(6) NOT NULL,
                `IsDeleted` tinyint(1) NOT NULL,
                CONSTRAINT `PK_Suppliers` PRIMARY KEY (`Id`),
                KEY `IX_Suppliers_InsertByUserId` (`InsertByUserId`),
                CONSTRAINT `FK_Suppliers_Users_InsertByUserId` FOREIGN KEY (`InsertByUserId`) REFERENCES `Users` (`Id`)
            ) CHARACTER SET utf8mb4;
            """);
    }

    public static void AddStringColumnIfNotExists(
        MigrationBuilder migrationBuilder,
        string table,
        string column,
        int maxLength)
    {
        migrationBuilder.Sql(
            $"""
            SET @col_exists := (
              SELECT COUNT(*) FROM information_schema.COLUMNS
              WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = '{table}' AND COLUMN_NAME = '{column}');
            SET @sql := IF(@col_exists = 0,
              'ALTER TABLE `{table}` ADD COLUMN `{column}` varchar({maxLength}) CHARACTER SET utf8mb4 NULL',
              'SELECT 1');
            PREPARE stmt FROM @sql;
            EXECUTE stmt;
            DEALLOCATE PREPARE stmt;
            """);
    }

    /// <summary>
    /// Adds a non-null tinyint(1) bool column with default 0 when missing (MySQL).
    /// </summary>
    public static void EnsureTinyIntBoolColumnIfNotExists(
        MigrationBuilder migrationBuilder,
        string table,
        string column)
    {
        migrationBuilder.Sql(EnsureTinyIntBoolColumnSql(table, column));
    }

    /// <summary>
    /// Same SQL as <see cref="EnsureTinyIntBoolColumnIfNotExists"/> for startup / ad-hoc use.
    /// </summary>
    public static string EnsureTinyIntBoolColumnSql(string table, string column)
    {
        return $"""
            SET @col_exists := (
              SELECT COUNT(*) FROM information_schema.COLUMNS
              WHERE TABLE_SCHEMA = DATABASE()
                AND LOWER(TABLE_NAME) = LOWER('{table}')
                AND LOWER(COLUMN_NAME) = LOWER('{column}'));
            SET @sql := IF(@col_exists = 0,
              'ALTER TABLE `{table}` ADD COLUMN `{column}` tinyint(1) NOT NULL DEFAULT 0',
              'SELECT 1');
            PREPARE stmt FROM @sql;
            EXECUTE stmt;
            DEALLOCATE PREPARE stmt;
            """;
    }
}
