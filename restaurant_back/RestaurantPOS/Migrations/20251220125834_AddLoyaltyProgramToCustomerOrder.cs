using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantPOS.Migrations
{
    /// <inheritdoc />
    public partial class AddLoyaltyProgramToCustomerOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "LoyaltyPrograms",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "DeliveryPointsEarned",
                table: "CustomerOrders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LoyaltyProgramId",
                table: "CustomerOrders",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrders_LoyaltyProgramId",
                table: "CustomerOrders",
                column: "LoyaltyProgramId");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerOrders_LoyaltyPrograms_LoyaltyProgramId",
                table: "CustomerOrders",
                column: "LoyaltyProgramId",
                principalTable: "LoyaltyPrograms",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomerOrders_LoyaltyPrograms_LoyaltyProgramId",
                table: "CustomerOrders");

            migrationBuilder.DropIndex(
                name: "IX_CustomerOrders_LoyaltyProgramId",
                table: "CustomerOrders");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "LoyaltyPrograms");

            migrationBuilder.DropColumn(
                name: "DeliveryPointsEarned",
                table: "CustomerOrders");

            migrationBuilder.DropColumn(
                name: "LoyaltyProgramId",
                table: "CustomerOrders");
        }
    }
}
