using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shekar.Infrastructure.Persistence.Migrations
{
    public partial class AddModelIdentity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DivarKey",
                table: "VehicleModels",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true,
                comment: "شناسه‌ی قطعیِ مدل در دیوار (brand_model)؛ یکتا؛ مبنای رجیستر خودکار");

            migrationBuilder.AddColumn<string>(
                name: "SourceModelKey",
                table: "CarListings",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true,
                comment: "شناسه‌ی قطعیِ مدل در منبع (brand_model دیوار)؛ مبنای نگاشت/رجیستر خودکار");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleModels_DivarKey",
                table: "VehicleModels",
                column: "DivarKey",
                unique: true,
                filter: "[DivarKey] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VehicleModels_DivarKey",
                table: "VehicleModels");

            migrationBuilder.DropColumn(
                name: "DivarKey",
                table: "VehicleModels");

            migrationBuilder.DropColumn(
                name: "SourceModelKey",
                table: "CarListings");
        }
    }
}
