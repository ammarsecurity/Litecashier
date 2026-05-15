using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RestaurantPOS.Db;

#nullable disable

namespace RestaurantPOS.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DbConfig))]
    [Migration("20260515120000_AddFloorPlanTableChipSize")]
    public partial class AddFloorPlanTableChipSize : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TableChipSizePx",
                table: "RestaurantLayoutSettings",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TableChipSizePx",
                table: "RestaurantLayoutSettings");
        }
    }
}
