using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantPOS.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderStatusFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OrderStatus",
                table: "CustomerOrders",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Pending")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PaymentStatus",
                table: "CustomerOrders",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Pending")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "DailySequenceNumber",
                table: "CustomerOrders",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrderStatus",
                table: "CustomerOrders");

            migrationBuilder.DropColumn(
                name: "PaymentStatus",
                table: "CustomerOrders");

            migrationBuilder.DropColumn(
                name: "DailySequenceNumber",
                table: "CustomerOrders");
        }
    }
}

