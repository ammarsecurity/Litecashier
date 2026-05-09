using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RestaurantPOS.Db;

#nullable disable

namespace RestaurantPOS.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DbConfig))]
    [Migration("20260509150000_RemoveEmailFromCustomer")]
    public partial class RemoveEmailFromCustomer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Email",
                table: "Customers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                    name: "Email",
                    table: "Customers",
                    type: "varchar(200)",
                    maxLength: 200,
                    nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
