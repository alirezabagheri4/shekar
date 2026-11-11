using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shekar.Infrastructure.Persistence.Migrations
{
    public partial class AddSegmentTrend : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TrendPercent",
                table: "MarketSegmentStats",
                type: "int",
                nullable: false,
                defaultValue: 0,
                comment: "روندِ قیمتِ پنجره (٪): مثبت=صعودی، منفی=نزولی");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TrendPercent",
                table: "MarketSegmentStats");
        }
    }
}
