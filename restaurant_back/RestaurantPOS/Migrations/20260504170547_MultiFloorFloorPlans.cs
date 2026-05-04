using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantPOS.Migrations
{
    /// <inheritdoc />
    public partial class MultiFloorFloorPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PlanKey",
                table: "RestaurantLayoutSettings",
                type: "varchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            // فهرس مركب أولاً (يغطي InsertByUserId لقيود الـ FK) ثم حذف الفهرس الفريد القديم
            migrationBuilder.CreateIndex(
                name: "IX_RestaurantLayoutSettings_InsertByUserId_PlanKey",
                table: "RestaurantLayoutSettings",
                columns: new[] { "InsertByUserId", "PlanKey" },
                unique: true);

            migrationBuilder.DropIndex(
                name: "IX_RestaurantLayoutSettings_InsertByUserId",
                table: "RestaurantLayoutSettings");

            migrationBuilder.CreateTable(
                name: "TableLayoutPlacements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TableId = table.Column<int>(type: "int", nullable: false),
                    PlanKey = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LayoutPosX = table.Column<double>(type: "double", nullable: false),
                    LayoutPosY = table.Column<double>(type: "double", nullable: false),
                    InsertDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdateDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TableLayoutPlacements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TableLayoutPlacements_Tables_TableId",
                        column: x => x.TableId,
                        principalTable: "Tables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_TableLayoutPlacements_TableId_PlanKey",
                table: "TableLayoutPlacements",
                columns: new[] { "TableId", "PlanKey" },
                unique: true);

            // نقل المواضع القديمة من Tables إلى صف لكل طاولة ومفتاح مخطط (Zone أو فارغ)
            migrationBuilder.Sql(@"
INSERT INTO `TableLayoutPlacements` (`TableId`, `PlanKey`, `LayoutPosX`, `LayoutPosY`, `InsertDate`, `UpdateDate`, `IsDeleted`)
SELECT
  `Id`,
  IF(IFNULL(TRIM(`Zone`), '') = '', '', TRIM(`Zone`)),
  `LayoutPosX`,
  `LayoutPosY`,
  UTC_TIMESTAMP(6),
  UTC_TIMESTAMP(6),
  0
FROM `Tables`
WHERE `LayoutPosX` IS NOT NULL AND `LayoutPosY` IS NOT NULL AND `IsDeleted` = 0;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TableLayoutPlacements");

            migrationBuilder.DropIndex(
                name: "IX_RestaurantLayoutSettings_InsertByUserId_PlanKey",
                table: "RestaurantLayoutSettings");

            migrationBuilder.DropColumn(
                name: "PlanKey",
                table: "RestaurantLayoutSettings");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantLayoutSettings_InsertByUserId",
                table: "RestaurantLayoutSettings",
                column: "InsertByUserId",
                unique: true);
        }
    }
}
