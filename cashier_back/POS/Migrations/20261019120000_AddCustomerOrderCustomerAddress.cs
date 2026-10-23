using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using POS.Db;

#nullable disable

namespace POS.Migrations
{
    [DbContext(typeof(DbConfig))]
    [Migration("20261019120000_AddCustomerOrderCustomerAddress")]
    public partial class AddCustomerOrderCustomerAddress : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                SET @col := (SELECT COUNT(*) FROM information_schema.COLUMNS
                  WHERE TABLE_SCHEMA = DATABASE() AND LOWER(TABLE_NAME) = 'customerorders' AND LOWER(COLUMN_NAME) = 'customeraddress');
                SET @sql := IF(@col = 0, 'ALTER TABLE `CustomerOrders` ADD COLUMN `CustomerAddress` varchar(500) NULL', 'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                SET @col := (SELECT COUNT(*) FROM information_schema.COLUMNS
                  WHERE TABLE_SCHEMA = DATABASE() AND LOWER(TABLE_NAME) = 'customerorders' AND LOWER(COLUMN_NAME) = 'customeraddress');
                SET @sql := IF(@col > 0, 'ALTER TABLE `CustomerOrders` DROP COLUMN `CustomerAddress`', 'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
                """);
        }
    }
}
