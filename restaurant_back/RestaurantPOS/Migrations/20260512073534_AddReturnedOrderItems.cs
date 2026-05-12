using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantPOS.Migrations
{
    /// <inheritdoc />
    public partial class AddReturnedOrderItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReturnedOrderItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CustomerOrderId = table.Column<int>(type: "int", nullable: false),
                    CustomerOrderItemId = table.Column<int>(type: "int", nullable: false),
                    TableId = table.Column<int>(type: "int", nullable: true),
                    ItemId = table.Column<int>(type: "int", nullable: false),
                    ItemName = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OrderCode = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TableNumber = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MergedTableNumbers = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OrderType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PaymentMethod = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LineTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Reason = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeletedByUserId = table.Column<int>(type: "int", nullable: false),
                    DeletedByUsername = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    InsertByUserId = table.Column<int>(type: "int", nullable: false),
                    InsertDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdateDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReturnedOrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReturnedOrderItems_CustomerOrderItems_CustomerOrderItemId",
                        column: x => x.CustomerOrderItemId,
                        principalTable: "CustomerOrderItems",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReturnedOrderItems_CustomerOrders_CustomerOrderId",
                        column: x => x.CustomerOrderId,
                        principalTable: "CustomerOrders",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReturnedOrderItems_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReturnedOrderItems_Tables_TableId",
                        column: x => x.TableId,
                        principalTable: "Tables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ReturnedOrderItems_Users_DeletedByUserId",
                        column: x => x.DeletedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReturnedOrderItems_Users_InsertByUserId",
                        column: x => x.InsertByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnedOrderItems_CustomerOrderId",
                table: "ReturnedOrderItems",
                column: "CustomerOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnedOrderItems_CustomerOrderItemId",
                table: "ReturnedOrderItems",
                column: "CustomerOrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnedOrderItems_DeletedByUserId",
                table: "ReturnedOrderItems",
                column: "DeletedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnedOrderItems_InsertByUserId",
                table: "ReturnedOrderItems",
                column: "InsertByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnedOrderItems_ItemId",
                table: "ReturnedOrderItems",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnedOrderItems_TableId",
                table: "ReturnedOrderItems",
                column: "TableId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReturnedOrderItems");
        }
    }
}
