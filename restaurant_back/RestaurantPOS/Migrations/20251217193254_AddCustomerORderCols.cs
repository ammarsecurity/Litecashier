using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantPOS.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerORderCols : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DailySequenceNumber",
                table: "CustomerOrders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrderStatus",
                table: "CustomerOrders",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PaymentStatus",
                table: "CustomerOrders",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DailySequenceNumber",
                table: "CustomerOrders");

            migrationBuilder.DropColumn(
                name: "OrderStatus",
                table: "CustomerOrders");

            migrationBuilder.DropColumn(
                name: "PaymentStatus",
                table: "CustomerOrders");
        }
    }
}
