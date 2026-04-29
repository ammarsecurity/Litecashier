using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantPOS.Migrations
{
    /// <inheritdoc />
    public partial class EnsureTagParentTagId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ParentTagId",
                table: "Tags",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tags_ParentTagId",
                table: "Tags",
                column: "ParentTagId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tags_Tags_ParentTagId",
                table: "Tags",
                column: "ParentTagId",
                principalTable: "Tags",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tags_Tags_ParentTagId",
                table: "Tags");

            migrationBuilder.DropIndex(
                name: "IX_Tags_ParentTagId",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "ParentTagId",
                table: "Tags");
        }
    }
}
