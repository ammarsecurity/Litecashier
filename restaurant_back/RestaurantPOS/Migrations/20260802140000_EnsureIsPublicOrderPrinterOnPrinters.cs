using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RestaurantPOS.Db;

#nullable disable

namespace RestaurantPOS.Migrations
{
    /// <summary>
    /// يضمن وجود عمود IsPublicOrderPrinter حتى لو سُجّل الترحيل السابق دون تطبيقه فعلياً.
    /// </summary>
    [DbContext(typeof(DbConfig))]
    [Migration("20260802140000_EnsureIsPublicOrderPrinterOnPrinters")]
    public partial class EnsureIsPublicOrderPrinterOnPrinters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            MigrationBootstrapSql.EnsureTinyIntBoolColumnIfNotExists(
                migrationBuilder,
                "Printers",
                "IsPublicOrderPrinter");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // لا نحذف العمود: قد تعتمد عليه قواعد بيانات أخرى
        }
    }
}
