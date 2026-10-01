using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using POS.Db;

#nullable disable

namespace POS.Migrations
{
    [DbContext(typeof(DbConfig))]
    [Migration("20261001140000_AddItemStockEntries")]
    public partial class AddItemStockEntries : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS `ItemStockEntries` (
                  `Id` int NOT NULL AUTO_INCREMENT,
                  `ItemId` int NOT NULL,
                  `WarehouseId` int NOT NULL,
                  `MovementType` varchar(10) CHARACTER SET utf8mb4 NOT NULL,
                  `Quantity` int NOT NULL,
                  `SellingPrice` decimal(18,4) NOT NULL,
                  `PurchasingPrice` decimal(18,4) NOT NULL,
                  `WholesalePrice` decimal(18,4) NOT NULL,
                  `DisCountPrice` decimal(18,4) NOT NULL,
                  `BatchPurchasingPrice` decimal(18,4) NULL,
                  `Notes` varchar(1000) CHARACTER SET utf8mb4 NULL,
                  `InsertByUserId` int NOT NULL,
                  `InsertDate` datetime(6) NOT NULL,
                  `UpdateDate` datetime(6) NOT NULL,
                  `IsDeleted` tinyint(1) NOT NULL DEFAULT 0,
                  PRIMARY KEY (`Id`),
                  KEY `IX_ItemStockEntries_ItemId_InsertDate` (`ItemId`, `InsertDate`),
                  KEY `IX_ItemStockEntries_WarehouseId` (`WarehouseId`),
                  KEY `IX_ItemStockEntries_InsertByUserId` (`InsertByUserId`),
                  CONSTRAINT `FK_ItemStockEntries_Items_ItemId` FOREIGN KEY (`ItemId`) REFERENCES `Items` (`Id`) ON DELETE RESTRICT,
                  CONSTRAINT `FK_ItemStockEntries_Warehouses_WarehouseId` FOREIGN KEY (`WarehouseId`) REFERENCES `Warehouses` (`Id`) ON DELETE RESTRICT,
                  CONSTRAINT `FK_ItemStockEntries_Users_InsertByUserId` FOREIGN KEY (`InsertByUserId`) REFERENCES `Users` (`Id`) ON DELETE RESTRICT
                ) CHARACTER SET=utf8mb4;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS `ItemStockEntries`;");
        }
    }
}
