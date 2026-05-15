using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RestaurantPOS.Db;

#nullable disable

namespace RestaurantPOS.Migrations
{
    /// <summary>ربط الصرفيات بفئة Tags (المبيعات حسب الفئة تستخدم e.TagId).</summary>
    [DbContext(typeof(DbConfig))]
    [Migration("20260515133000_AddExpenseTagId")]
    public partial class AddExpenseTagId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TagId",
                table: "Expenses",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_TagId",
                table: "Expenses",
                column: "TagId");

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_Tags_TagId",
                table: "Expenses",
                column: "TagId",
                principalTable: "Tags",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_Tags_TagId",
                table: "Expenses");

            migrationBuilder.DropIndex(
                name: "IX_Expenses_TagId",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "TagId",
                table: "Expenses");
        }
    }
}
