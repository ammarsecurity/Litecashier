using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantPOS.Migrations
{
    /// <inheritdoc />
    public partial class AddStockMovementReceivedByEmployeeName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            MigrationBootstrapSql.EnsureStockMovementsTable(migrationBuilder);

            MigrationBootstrapSql.AddStringColumnIfNotExists(
                migrationBuilder, "StockMovements", "ReceivedByEmployeeName", 200);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReceivedByEmployeeName",
                table: "StockMovements");
        }
    }
}
