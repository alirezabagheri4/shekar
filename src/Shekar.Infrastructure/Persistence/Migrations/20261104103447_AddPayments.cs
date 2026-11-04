using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shekar.Infrastructure.Persistence.Migrations
{
    public partial class AddPayments : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AiSummary",
                table: "Hunts",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                comment: "خلاصه هوش مصنوعی");

            migrationBuilder.AddColumn<long>(
                name: "BasePriceToman",
                table: "Hunts",
                type: "bigint",
                nullable: true,
                comment: "قیمت پایه به تومان (قیمت صفر/کارخانه)");

            migrationBuilder.AddColumn<string>(
                name: "BodyConditionText",
                table: "Hunts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                comment: "وضعیت بدنه به متن");

            migrationBuilder.AddColumn<string>(
                name: "BrandModel",
                table: "Hunts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                comment: "برند و مدل، مثل «پژو ۲۰۷ پانوراما»");

            migrationBuilder.AddColumn<string>(
                name: "ChassisCondition",
                table: "Hunts",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true,
                comment: "وضعیت شاسی");

            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "Hunts",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true,
                comment: "رنگ خودرو");

            migrationBuilder.AddColumn<string>(
                name: "EngineCondition",
                table: "Hunts",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true,
                comment: "وضعیت موتور");

            migrationBuilder.AddColumn<string>(
                name: "FuelType",
                table: "Hunts",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                comment: "نوع سوخت: بنزینی/دوگانه‌سوز/برقی/هیبرید");

            migrationBuilder.AddColumn<string>(
                name: "GearboxCondition",
                table: "Hunts",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true,
                comment: "وضعیت گیربکس");

            migrationBuilder.AddColumn<string>(
                name: "GearboxType",
                table: "Hunts",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                comment: "نوع گیربکس: دنده‌ای/اتوماتیک");

            migrationBuilder.AddColumn<int>(
                name: "ImageCount",
                table: "Hunts",
                type: "int",
                nullable: false,
                defaultValue: 0,
                comment: "تعداد تصاویر آگهی");

            migrationBuilder.AddColumn<int>(
                name: "InsuranceMonths",
                table: "Hunts",
                type: "int",
                nullable: true,
                comment: "ماه‌های باقی‌مانده بیمه شخص ثالث");

            migrationBuilder.AddColumn<int>(
                name: "ResellDaysMax",
                table: "Hunts",
                type: "int",
                nullable: true,
                comment: "حداکثر روز برای فروش مجدد");

            migrationBuilder.AddColumn<int>(
                name: "ResellDaysMin",
                table: "Hunts",
                type: "int",
                nullable: true,
                comment: "حداقل روز برای فروش مجدد");

            migrationBuilder.AddColumn<string>(
                name: "SellerDescription",
                table: "Hunts",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                comment: "توضیحات فروشنده");

            migrationBuilder.AddColumn<string>(
                name: "SellerName",
                table: "Hunts",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true,
                comment: "نام فروشنده");

            migrationBuilder.AddColumn<string>(
                name: "SourceUrl",
                table: "Hunts",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true,
                comment: "لینک آگهی منبع");

            migrationBuilder.AddColumn<int>(
                name: "Score",
                table: "HuntAnalysisFactors",
                type: "int",
                nullable: false,
                defaultValue: 0,
                comment: "امتیاز عددی سنجه ۰ تا ۱۰۰");

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false, comment: "کاربر پرداخت‌کننده"),
                    Mobile = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false, comment: "شماره موبایل کاربر در لحظه‌ی پرداخت (مبنای بلاک)"),
                    PlanType = table.Column<byte>(type: "tinyint", nullable: false, comment: "پلن خریداری‌شده: 1=شکارچی،2=حرفه‌ای"),
                    CityId = table.Column<int>(type: "int", nullable: false, comment: "شهر اشتراک (مبنای ظرفیت R1)"),
                    AmountToman = table.Column<long>(type: "bigint", nullable: false, comment: "مبلغ واریزی به تومان"),
                    Method = table.Column<byte>(type: "tinyint", nullable: false, comment: "روش رسید: 1=عکس،2=متن"),
                    ReceiptImageKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "کلید فایل رسید (خارج از wwwroot عمومی)"),
                    ReceiptText = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true, comment: "متن رسید (روش متن)"),
                    Reference = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false, comment: "کد پیگیری نمایشی، مثل SHK-482193"),
                    Ip = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true, comment: "IP ثبت‌کننده‌ی پرداخت"),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, comment: "زمان ثبت پرداخت"),
                    Status = table.Column<byte>(type: "tinyint", nullable: false, comment: "وضعیت بررسی: 1=در انتظار،2=تأیید،3=رد"),
                    SubscriptionId = table.Column<int>(type: "int", nullable: true, comment: "اشتراک فعال/تمدیدشده با این پرداخت (برای غیرفعال‌سازی هنگام رد)"),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true, comment: "زمان بررسی ادمین"),
                    ReviewedByUserId = table.Column<int>(type: "int", nullable: true, comment: "ادمین بررسی‌کننده"),
                    ReviewNote = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true, comment: "توضیح بررسی (مثلاً دلیل رد)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                },
                comment: "پرداخت‌های کارت‌به‌کارت؛ اشتراک فوری فعال و منتظر تأیید/رد ادمین");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Status",
                table: "Payments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_UserId_Status",
                table: "Payments",
                columns: new[] { "UserId", "Status" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropColumn(
                name: "AiSummary",
                table: "Hunts");

            migrationBuilder.DropColumn(
                name: "BasePriceToman",
                table: "Hunts");

            migrationBuilder.DropColumn(
                name: "BodyConditionText",
                table: "Hunts");

            migrationBuilder.DropColumn(
                name: "BrandModel",
                table: "Hunts");

            migrationBuilder.DropColumn(
                name: "ChassisCondition",
                table: "Hunts");

            migrationBuilder.DropColumn(
                name: "Color",
                table: "Hunts");

            migrationBuilder.DropColumn(
                name: "EngineCondition",
                table: "Hunts");

            migrationBuilder.DropColumn(
                name: "FuelType",
                table: "Hunts");

            migrationBuilder.DropColumn(
                name: "GearboxCondition",
                table: "Hunts");

            migrationBuilder.DropColumn(
                name: "GearboxType",
                table: "Hunts");

            migrationBuilder.DropColumn(
                name: "ImageCount",
                table: "Hunts");

            migrationBuilder.DropColumn(
                name: "InsuranceMonths",
                table: "Hunts");

            migrationBuilder.DropColumn(
                name: "ResellDaysMax",
                table: "Hunts");

            migrationBuilder.DropColumn(
                name: "ResellDaysMin",
                table: "Hunts");

            migrationBuilder.DropColumn(
                name: "SellerDescription",
                table: "Hunts");

            migrationBuilder.DropColumn(
                name: "SellerName",
                table: "Hunts");

            migrationBuilder.DropColumn(
                name: "SourceUrl",
                table: "Hunts");

            migrationBuilder.DropColumn(
                name: "Score",
                table: "HuntAnalysisFactors");
        }
    }
}
