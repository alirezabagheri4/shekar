using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shekar.Infrastructure.Persistence.Migrations
{
    public partial class AddModelLiquidityStat : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ModelLiquidityStats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VehicleModelId = table.Column<int>(type: "int", nullable: false, comment: "مدل خودرو (کلیدِ یکتا)"),
                    WindowDays = table.Column<int>(type: "int", nullable: false, comment: "طول پنجره‌ی نمونه‌گیری (روز)"),
                    SampleCount = table.Column<int>(type: "int", nullable: false, comment: "کلِ آگهی‌های دیده‌شده‌ی مدل در پنجره (کوهورت)"),
                    RemovedCount = table.Column<int>(type: "int", nullable: false, comment: "تعداد حذف‌شده (پراکسیِ فروش؛ مبنای DOM)"),
                    ActiveCount = table.Column<int>(type: "int", nullable: false, comment: "تعداد هنوز فعال (لحاظِ سانسور)"),
                    MedianDomDays = table.Column<int>(type: "int", nullable: false, comment: "میانه‌ی روزهای‌روی‌بازار (کمتر=نقدشونده‌تر)"),
                    P25DomDays = table.Column<int>(type: "int", nullable: false, comment: "چارک اولِ روزهای‌روی‌بازار"),
                    RemovalRatePercent = table.Column<int>(type: "int", nullable: false, comment: "نرخِ حذف در پنجره (٪)"),
                    IsReliable = table.Column<bool>(type: "bit", nullable: false, comment: "آیا نقدشوندگیِ داده‌محور قابل‌اتکاست (حذفِ کافی)"),
                    ComputedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, comment: "زمان آخرین محاسبه")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelLiquidityStats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModelLiquidityStats_VehicleModels_VehicleModelId",
                        column: x => x.VehicleModelId,
                        principalTable: "VehicleModels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "آمارِ غلتانِ نقدشوندگیِ هر مدل (DOM/نرخِ حذف)؛ داده‌ی گامِ اسکورینگ");

            migrationBuilder.CreateIndex(
                name: "IX_ModelLiquidityStats_VehicleModelId",
                table: "ModelLiquidityStats",
                column: "VehicleModelId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ModelLiquidityStats");
        }
    }
}
