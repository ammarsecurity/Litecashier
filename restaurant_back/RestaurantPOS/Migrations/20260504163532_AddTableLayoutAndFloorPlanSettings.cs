using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantPOS.Migrations
{
    /// <inheritdoc />
    public partial class AddTableLayoutAndFloorPlanSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "LayoutPosX",
                table: "Tables",
                type: "double",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "LayoutPosY",
                table: "Tables",
                type: "double",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RestaurantLayoutSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    InsertByUserId = table.Column<int>(type: "int", nullable: false),
                    FloorPlanImageFileName = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BackgroundColor = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ZonesJson = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    InsertDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdateDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestaurantLayoutSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RestaurantLayoutSettings_Users_InsertByUserId",
                        column: x => x.InsertByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantLayoutSettings_InsertByUserId",
                table: "RestaurantLayoutSettings",
                column: "InsertByUserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RestaurantLayoutSettings");

            migrationBuilder.DropColumn(
                name: "LayoutPosX",
                table: "Tables");

            migrationBuilder.DropColumn(
                name: "LayoutPosY",
                table: "Tables");
        }
    }
}
