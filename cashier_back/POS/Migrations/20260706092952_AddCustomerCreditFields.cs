using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerCreditFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CreditCustomerId",
                table: "CustomerOrders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentStatus",
                table: "CustomerOrders",
                type: "longtext",
                nullable: false,
                defaultValue: "Pending")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "SettlementPaymentMethod",
                table: "CustomerOrders",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "SettledAt",
                table: "CustomerOrders",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrders_CreditCustomerId",
                table: "CustomerOrders",
                column: "CreditCustomerId");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerOrders_Customers_CreditCustomerId",
                table: "CustomerOrders",
                column: "CreditCustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.Sql(
                "UPDATE CustomerOrders SET PaymentStatus = 'Paid' WHERE PaymentMethod IS NULL OR PaymentMethod <> 'Credit'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomerOrders_Customers_CreditCustomerId",
                table: "CustomerOrders");

            migrationBuilder.DropIndex(
                name: "IX_CustomerOrders_CreditCustomerId",
                table: "CustomerOrders");

            migrationBuilder.DropColumn(
                name: "CreditCustomerId",
                table: "CustomerOrders");

            migrationBuilder.DropColumn(
                name: "PaymentStatus",
                table: "CustomerOrders");

            migrationBuilder.DropColumn(
                name: "SettlementPaymentMethod",
                table: "CustomerOrders");

            migrationBuilder.DropColumn(
                name: "SettledAt",
                table: "CustomerOrders");
        }
    }
}
