using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantPOS.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderDiscountFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "CustomerOrders",
                type: "decimal(65,30)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountPercent",
                table: "CustomerOrders",
                type: "decimal(65,30)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DiscountType",
                table: "CustomerOrders",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountValue",
                table: "CustomerOrders",
                type: "decimal(65,30)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OrderSubTotal",
                table: "CustomerOrders",
                type: "decimal(65,30)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OrderTotalAfterDiscount",
                table: "CustomerOrders",
                type: "decimal(65,30)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "CustomerOrders");

            migrationBuilder.DropColumn(
                name: "DiscountPercent",
                table: "CustomerOrders");

            migrationBuilder.DropColumn(
                name: "DiscountType",
                table: "CustomerOrders");

            migrationBuilder.DropColumn(
                name: "DiscountValue",
                table: "CustomerOrders");

            migrationBuilder.DropColumn(
                name: "OrderSubTotal",
                table: "CustomerOrders");

            migrationBuilder.DropColumn(
                name: "OrderTotalAfterDiscount",
                table: "CustomerOrders");
        }
    }
}
