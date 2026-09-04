using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using POS.Db;

#nullable disable

namespace POS.Migrations
{
    [DbContext(typeof(DbConfig))]
    [Migration("20260904120000_AddPublicMenuAds")]
    public partial class AddPublicMenuAds : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS `PublicMenuAds` (
                  `Id` int NOT NULL AUTO_INCREMENT,
                  `CommercialUserId` int NOT NULL,
                  `Image` longtext NOT NULL,
                  `Title` varchar(120) NULL,
                  `SortOrder` int NOT NULL,
                  `IsActive` tinyint(1) NOT NULL,
                  `InsertDate` datetime(6) NOT NULL,
                  `UpdateDate` datetime(6) NOT NULL,
                  `IsDeleted` tinyint(1) NOT NULL,
                  PRIMARY KEY (`Id`),
                  KEY `IX_PublicMenuAds_CommercialUserId_SortOrder` (`CommercialUserId`, `SortOrder`),
                  CONSTRAINT `FK_PublicMenuAds_Users_CommercialUserId`
                    FOREIGN KEY (`CommercialUserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
                ) CHARACTER SET utf8mb4;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS `PublicMenuAds`;");
        }
    }
}
