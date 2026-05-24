using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RestaurantPOS.Db;

#nullable disable

namespace RestaurantPOS.Migrations
{
    [DbContext(typeof(DbConfig))]
    [Migration("20260521100000_AddNotesToCustomerOrderItem")]
    public partial class AddNotesToCustomerOrderItem : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "CustomerOrderItems",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Notes",
                table: "CustomerOrderItems");
        }
    }
}
