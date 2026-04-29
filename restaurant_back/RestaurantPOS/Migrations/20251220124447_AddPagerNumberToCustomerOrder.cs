using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantPOS.Migrations
{
    /// <inheritdoc />
    public partial class AddPagerNumberToCustomerOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeliveryAddress",
                table: "CustomerOrders",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveryAssignedAt",
                table: "CustomerOrders",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveryCompletedAt",
                table: "CustomerOrders",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryCustomerName",
                table: "CustomerOrders",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "DeliveryDriverId",
                table: "CustomerOrders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DeliveryFee",
                table: "CustomerOrders",
                type: "decimal(65,30)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryPhoneNumber",
                table: "CustomerOrders",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "DeliveryStatus",
                table: "CustomerOrders",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PagerNumber",
                table: "CustomerOrders",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DeliveryDrivers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PhoneNumber = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Address = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    VehicleType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    VehicleNumber = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Notes = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    InsertByUserId = table.Column<int>(type: "int", nullable: false),
                    InsertDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdateDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryDrivers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeliveryDrivers_Users_InsertByUserId",
                        column: x => x.InsertByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrders_DeliveryDriverId",
                table: "CustomerOrders",
                column: "DeliveryDriverId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryDrivers_InsertByUserId",
                table: "DeliveryDrivers",
                column: "InsertByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerOrders_DeliveryDrivers_DeliveryDriverId",
                table: "CustomerOrders",
                column: "DeliveryDriverId",
                principalTable: "DeliveryDrivers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomerOrders_DeliveryDrivers_DeliveryDriverId",
                table: "CustomerOrders");

            migrationBuilder.DropTable(
                name: "DeliveryDrivers");

            migrationBuilder.DropIndex(
                name: "IX_CustomerOrders_DeliveryDriverId",
                table: "CustomerOrders");

            migrationBuilder.DropColumn(
                name: "DeliveryAddress",
                table: "CustomerOrders");

            migrationBuilder.DropColumn(
                name: "DeliveryAssignedAt",
                table: "CustomerOrders");

            migrationBuilder.DropColumn(
                name: "DeliveryCompletedAt",
                table: "CustomerOrders");

            migrationBuilder.DropColumn(
                name: "DeliveryCustomerName",
                table: "CustomerOrders");

            migrationBuilder.DropColumn(
                name: "DeliveryDriverId",
                table: "CustomerOrders");

            migrationBuilder.DropColumn(
                name: "DeliveryFee",
                table: "CustomerOrders");

            migrationBuilder.DropColumn(
                name: "DeliveryPhoneNumber",
                table: "CustomerOrders");

            migrationBuilder.DropColumn(
                name: "DeliveryStatus",
                table: "CustomerOrders");

            migrationBuilder.DropColumn(
                name: "PagerNumber",
                table: "CustomerOrders");
        }
    }
}
