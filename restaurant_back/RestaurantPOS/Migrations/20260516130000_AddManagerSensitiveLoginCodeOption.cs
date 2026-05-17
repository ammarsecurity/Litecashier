using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RestaurantPOS.Db;

#nullable disable

namespace RestaurantPOS.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DbConfig))]
    [Migration("20260516130000_AddManagerSensitiveLoginCodeOption")]
    public partial class AddManagerSensitiveLoginCodeOption : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CanUseOwnLoginCodeForSensitiveActions",
                table: "Users",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CanUseOwnLoginCodeForSensitiveActions",
                table: "Users");
        }
    }
}
