using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RestaurantPOS.Db;

#nullable disable

namespace RestaurantPOS.Migrations
{
    [DbContext(typeof(DbConfig))]
    [Migration("20260519120000_AddHiddenFromQueueDisplayToCustomerOrder")]
    public partial class AddHiddenFromQueueDisplayToCustomerOrder : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HiddenFromQueueDisplay",
                table: "CustomerOrders",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HiddenFromQueueDisplay",
                table: "CustomerOrders");
        }
    }
}
