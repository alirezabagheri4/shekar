using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shekar.Infrastructure.Persistence.Migrations
{
    public partial class AddMileageTrend : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "MileageSlopeTomanPerK",
                table: "MarketSegmentStats",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                comment: "شیبِ قیمت به‌ازای هر ۱۰۰۰ کیلومتر (تومان، معمولاً منفی)");

            migrationBuilder.AddColumn<int>(
                name: "RefMileageKm",
                table: "MarketSegmentStats",
                type: "int",
                nullable: false,
                defaultValue: 0,
                comment: "کارکردِ مرجع (میانه‌ی نمونه)؛ مبنای تعدیلِ کارکرد");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MileageSlopeTomanPerK",
                table: "MarketSegmentStats");

            migrationBuilder.DropColumn(
                name: "RefMileageKm",
                table: "MarketSegmentStats");
        }
    }
}
