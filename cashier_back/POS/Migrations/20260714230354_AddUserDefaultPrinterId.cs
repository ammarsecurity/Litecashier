using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS.Migrations
{
    /// <inheritdoc />
    public partial class AddUserDefaultPrinterId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DefaultPrinterId",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "DefaultPrinterId",
                value: null);

            migrationBuilder.CreateIndex(
                name: "IX_Users_DefaultPrinterId",
                table: "Users",
                column: "DefaultPrinterId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Printers_DefaultPrinterId",
                table: "Users",
                column: "DefaultPrinterId",
                principalTable: "Printers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Printers_DefaultPrinterId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_DefaultPrinterId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DefaultPrinterId",
                table: "Users");
        }
    }
}
