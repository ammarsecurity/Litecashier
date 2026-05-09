using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RestaurantPOS.Db;

#nullable disable

namespace RestaurantPOS.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DbConfig))]
    [Migration("20260509170000_AddCreditAccountToCustomerOrder")]
    public partial class AddCreditAccountToCustomerOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CreditCustomerId",
                table: "CustomerOrders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreditEmployeeId",
                table: "CustomerOrders",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrders_CreditCustomerId",
                table: "CustomerOrders",
                column: "CreditCustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrders_CreditEmployeeId",
                table: "CustomerOrders",
                column: "CreditEmployeeId");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerOrders_Customers_CreditCustomerId",
                table: "CustomerOrders",
                column: "CreditCustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerOrders_Employees_CreditEmployeeId",
                table: "CustomerOrders",
                column: "CreditEmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomerOrders_Customers_CreditCustomerId",
                table: "CustomerOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_CustomerOrders_Employees_CreditEmployeeId",
                table: "CustomerOrders");

            migrationBuilder.DropIndex(
                name: "IX_CustomerOrders_CreditCustomerId",
                table: "CustomerOrders");

            migrationBuilder.DropIndex(
                name: "IX_CustomerOrders_CreditEmployeeId",
                table: "CustomerOrders");

            migrationBuilder.DropColumn(
                name: "CreditCustomerId",
                table: "CustomerOrders");

            migrationBuilder.DropColumn(
                name: "CreditEmployeeId",
                table: "CustomerOrders");
        }
    }
}
