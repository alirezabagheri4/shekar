using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shekar.Infrastructure.Persistence.Migrations
{
    public partial class AddSegmentStatQuality : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "SampleCount",
                table: "MarketSegmentStats",
                type: "int",
                nullable: false,
                comment: "تعداد نمونه‌ی به‌کاررفته (پس از تریم)",
                oldClrType: typeof(int),
                oldType: "int",
                oldComment: "تعداد نمونه در پنجره");

            migrationBuilder.AddColumn<bool>(
                name: "IsReliable",
                table: "MarketSegmentStats",
                type: "bit",
                nullable: false,
                defaultValue: false,
                comment: "آیا سگمنت قابل‌اتکاست (نمونه‌ی کافی + پراکندگیِ محدود + تازه)");

            migrationBuilder.AddColumn<long>(
                name: "MinPriceToman",
                table: "MarketSegmentStats",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                comment: "کمینه‌ی قیمتِ تریم‌شده");

            migrationBuilder.AddColumn<int>(
                name: "RawSampleCount",
                table: "MarketSegmentStats",
                type: "int",
                nullable: false,
                defaultValue: 0,
                comment: "تعداد نمونه‌ی خام (پیش از تریم)");

            migrationBuilder.AddColumn<int>(
                name: "SpreadPercent",
                table: "MarketSegmentStats",
                type: "int",
                nullable: false,
                defaultValue: 0,
                comment: "پراکندگیِ نسبی IQR÷میانه (٪)");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsReliable",
                table: "MarketSegmentStats");

            migrationBuilder.DropColumn(
                name: "MinPriceToman",
                table: "MarketSegmentStats");

            migrationBuilder.DropColumn(
                name: "RawSampleCount",
                table: "MarketSegmentStats");

            migrationBuilder.DropColumn(
                name: "SpreadPercent",
                table: "MarketSegmentStats");

            migrationBuilder.AlterColumn<int>(
                name: "SampleCount",
                table: "MarketSegmentStats",
                type: "int",
                nullable: false,
                comment: "تعداد نمونه در پنجره",
                oldClrType: typeof(int),
                oldType: "int",
                oldComment: "تعداد نمونه‌ی به‌کاررفته (پس از تریم)");
        }
    }
}
