using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantPOS.Migrations
{
    /// <inheritdoc />
    public partial class AddStockMovementReceiptNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            MigrationBootstrapSql.EnsureEmployeesTable(migrationBuilder);
            MigrationBootstrapSql.EnsureExpensesEmployeeId(migrationBuilder);
            MigrationBootstrapSql.EnsureStockMovementsTable(migrationBuilder);
            MigrationBootstrapSql.EnsureSuppliersTable(migrationBuilder);

            MigrationBootstrapSql.AddStringColumnIfNotExists(
                migrationBuilder, "StockMovements", "ReceiptNumber", 200);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReceiptNumber",
                table: "StockMovements");
        }
    }
}
