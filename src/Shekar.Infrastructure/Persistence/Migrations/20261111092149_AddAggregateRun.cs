using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shekar.Infrastructure.Persistence.Migrations
{
    public partial class AddAggregateRun : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AggregateRuns",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, comment: "زمان شروع"),
                    FinishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true, comment: "زمان پایان"),
                    Status = table.Column<byte>(type: "tinyint", nullable: false, comment: "وضعیت: 1=درحال‌اجرا،2=موفق،3=خطا"),
                    NormalizedCount = table.Column<int>(type: "int", nullable: false, comment: "تعداد آگهیِ نرمال‌شده"),
                    AutoRegistered = table.Column<int>(type: "int", nullable: false, comment: "تعداد مدلِ رجیسترِ خودکار"),
                    UnmatchedCount = table.Column<int>(type: "int", nullable: false, comment: "تعداد بی‌تطبیق"),
                    SegmentCount = table.Column<int>(type: "int", nullable: false, comment: "تعداد کلِ سگمنت"),
                    ReliableSegmentCount = table.Column<int>(type: "int", nullable: false, comment: "تعداد سگمنتِ قابل‌اتکا"),
                    StaledCount = table.Column<int>(type: "int", nullable: false, comment: "تعداد سگمنتِ کهنه‌شده"),
                    Error = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "پیام خطا")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AggregateRuns", x => x.Id);
                },
                comment: "لاگ هر پاس اگریگیشن (نرمال‌سازی + ساخت سگمنت) برای پایش");

            migrationBuilder.CreateIndex(
                name: "IX_AggregateRuns_StartedAt",
                table: "AggregateRuns",
                column: "StartedAt");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AggregateRuns");
        }
    }
}
