using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shekar.Infrastructure.Persistence.Migrations
{
    public partial class AddSegmentLevel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "Level",
                table: "MarketSegmentStats",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0,
                comment: "سطحِ سلسله‌مراتب: 0=دقیق،1=بدونِ‌رنگ،2=بدونِ‌بدنه");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Level",
                table: "MarketSegmentStats");
        }
    }
}
