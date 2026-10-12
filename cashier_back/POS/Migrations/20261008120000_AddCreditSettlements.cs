using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using POS.Db;

#nullable disable

namespace POS.Migrations
{
    [DbContext(typeof(DbConfig))]
    [Migration("20261008120000_AddCreditSettlements")]
    public partial class AddCreditSettlements : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                SET @col_exists := (
                  SELECT COUNT(*) FROM information_schema.COLUMNS
                  WHERE TABLE_SCHEMA = DATABASE()
                    AND LOWER(TABLE_NAME) = 'customerorders'
                    AND LOWER(COLUMN_NAME) = 'settledamount');
                SET @sql := IF(@col_exists = 0,
                  'ALTER TABLE `CustomerOrders` ADD COLUMN `SettledAmount` decimal(18,2) NOT NULL DEFAULT 0',
                  'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

                UPDATE `CustomerOrders`
                SET `SettledAmount` = COALESCE(`OrderTotalAfterDiscount`, `OrderSubTotal`, 0)
                WHERE `PaymentMethod` = 'Credit'
                  AND `PaymentStatus` = 'Paid'
                  AND (`SettledAmount` IS NULL OR `SettledAmount` = 0);

                CREATE TABLE IF NOT EXISTS `CreditSettlements` (
                  `Id` int NOT NULL AUTO_INCREMENT,
                  `CustomerOrderId` int NOT NULL,
                  `Amount` decimal(18,2) NOT NULL,
                  `PaymentMethod` varchar(20) NOT NULL,
                  `Notes` varchar(1000) NULL,
                  `InsertByUserId` int NOT NULL,
                  `InsertDate` datetime(6) NOT NULL,
                  `UpdateDate` datetime(6) NOT NULL,
                  `IsDeleted` tinyint(1) NOT NULL DEFAULT 0,
                  PRIMARY KEY (`Id`),
                  KEY `IX_CreditSettlements_CustomerOrderId` (`CustomerOrderId`),
                  KEY `IX_CreditSettlements_InsertByUserId` (`InsertByUserId`),
                  CONSTRAINT `FK_CreditSettlements_CustomerOrders_CustomerOrderId`
                    FOREIGN KEY (`CustomerOrderId`) REFERENCES `CustomerOrders` (`Id`) ON DELETE CASCADE,
                  CONSTRAINT `FK_CreditSettlements_Users_InsertByUserId`
                    FOREIGN KEY (`InsertByUserId`) REFERENCES `Users` (`Id`) ON DELETE RESTRICT
                ) CHARACTER SET=utf8mb4;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TABLE IF EXISTS `CreditSettlements`;

                SET @col_exists := (
                  SELECT COUNT(*) FROM information_schema.COLUMNS
                  WHERE TABLE_SCHEMA = DATABASE()
                    AND LOWER(TABLE_NAME) = 'customerorders'
                    AND LOWER(COLUMN_NAME) = 'settledamount');
                SET @sql := IF(@col_exists > 0,
                  'ALTER TABLE `CustomerOrders` DROP COLUMN `SettledAmount`',
                  'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
                """);
        }
    }
}
