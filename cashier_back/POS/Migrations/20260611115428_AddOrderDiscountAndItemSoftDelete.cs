using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderDiscountAndItemSoftDelete : Migration
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

            migrationBuilder.AddColumn<DateTime>(
                name: "InsertDate",
                table: "CustomerOrderItems",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "CustomerOrderItems",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "CustomerOrderItems",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdateDate",
                table: "CustomerOrderItems",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
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

            migrationBuilder.DropColumn(
                name: "InsertDate",
                table: "CustomerOrderItems");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "CustomerOrderItems");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "CustomerOrderItems");

            migrationBuilder.DropColumn(
                name: "UpdateDate",
                table: "CustomerOrderItems");
        }
    }
}
