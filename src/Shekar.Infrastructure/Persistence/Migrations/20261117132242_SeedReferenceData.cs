using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shekar.Infrastructure.Persistence.Migrations
{
    public partial class SeedReferenceData : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Cities",
                columns: new[] { "Id", "Capacity", "IsActive", "Name", "Slug" },
                values: new object[,]
                {
                    { 1, 100, true, "تهران", "tehran" },
                    { 2, 100, false, "کرج", "karaj" },
                    { 3, 100, false, "اصفهان", "isfahan" },
                    { 4, 100, false, "مشهد", "mashhad" }
                });

            migrationBuilder.InsertData(
                table: "Plans",
                columns: new[] { "Id", "MonthlyPriceToman", "Name", "Type" },
                values: new object[,]
                {
                    { 1, 990000L, "شکارچی", (byte)1 },
                    { 2, 1499000L, "شکارچی حرفه‌ای", (byte)2 }
                });

            migrationBuilder.InsertData(
                table: "SystemSettings",
                columns: new[] { "Key", "Description", "UpdatedAt", "Value", "ValueType" },
                values: new object[,]
                {
                    { "BellAlertHours", "R6 — مدت فعال‌بودن زنگوله پیش از خاموشی خودکار (ساعت)", new DateTimeOffset(new DateTime(2026, 11, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "24", "int" },
                    { "CardTransfer.BankName", "نام بانک کارت مقصد", new DateTimeOffset(new DateTime(2026, 11, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "بانک سامان", "string" },
                    { "CardTransfer.CardNumber", "شماره کارت مقصد پرداخت کارت‌به‌کارت", new DateTimeOffset(new DateTime(2026, 11, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "6219861082608372", "string" },
                    { "CardTransfer.HolderName", "نام صاحب کارت مقصد", new DateTimeOffset(new DateTime(2026, 11, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "علیرضا باقری", "string" },
                    { "CityCapacity", "R1 — ظرفیت پیش‌فرض اشتراک فعال هر شهر", new DateTimeOffset(new DateTime(2026, 11, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "100", "int" },
                    { "FreeAppraisalsPerMonthGuest", "R5 — سهمیه‌ی کارشناسی رایگان ماهانه برای کاربر بدون اشتراک", new DateTimeOffset(new DateTime(2026, 11, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "3", "int" },
                    { "GoldenScoreThreshold", "R8 — حداقل امتیاز برای «فرصت طلایی»", new DateTimeOffset(new DateTime(2026, 11, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "85", "int" },
                    { "MaxAdsPerMonthBeforeReview", "آستانه‌ی تعداد آگهی ماهانه برای بررسی دلال/اسپم", new DateTimeOffset(new DateTime(2026, 11, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "20", "int" },
                    { "SupportEndHour", "R12 — ساعت پایان پشتیبانی (Asia/Tehran)", new DateTimeOffset(new DateTime(2026, 11, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "18", "int" },
                    { "SupportStartHour", "R12 — ساعت شروع پشتیبانی (Asia/Tehran)", new DateTimeOffset(new DateTime(2026, 11, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "9", "int" },
                    { "TransactionCostFloorToman", "R9 — کف هزینه‌ی نقل‌وانتقال و فروش (تومان)", new DateTimeOffset(new DateTime(2026, 11, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "15000000", "int" },
                    { "TransactionCostRate", "R9 — نرخ هزینه‌ی نقل‌وانتقال نسبت به قیمت آگهی", new DateTimeOffset(new DateTime(2026, 11, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "0.02", "decimal" },
                    { "TrialHours", "R3 — مدت اشتراک آزمایشی رایگان (ساعت)", new DateTimeOffset(new DateTime(2026, 11, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "24", "int" }
                });

            migrationBuilder.InsertData(
                table: "VehicleModels",
                columns: new[] { "Id", "Aliases", "BodyType", "Brand", "ColorSensitive", "DivarKey", "IsActive", "LiquidityScore", "Model", "Notes", "Trim" },
                values: new object[,]
                {
                    { 1, "[\"207\",\"\\u067E\\u0698\\u0648 207\",\"peugeot 207\"]", "هاچ‌بک", "پژو", false, null, true, 90, "۲۰۷", null, "دستی" },
                    { 2, "[\"206\",\"\\u067E\\u0698\\u0648 206\"]", "هاچ‌بک", "پژو", false, null, true, 85, "۲۰۶", null, "تیپ ۵" },
                    { 3, "[\"pars\",\"\\u067E\\u0698\\u0648 \\u067E\\u0627\\u0631\\u0633\"]", "سدان", "پژو", false, null, true, 80, "پارس", null, null },
                    { 4, "[\"pride\",\"\\u067E\\u0631\\u0627\\u06CC\\u062F 111\"]", "هاچ‌بک", "سایپا", false, null, true, 88, "پراید", null, "۱۱۱" },
                    { 5, "[\"tiba\",\"\\u062A\\u06CC\\u0628\\u0627\"]", "سدان", "سایپا", false, null, true, 78, "تیبا", null, null },
                    { 6, "[\"quick\",\"\\u06A9\\u0648\\u06CC\\u06CC\\u06A9 \\u06A9\\u0631\\u0627\\u0633\"]", "هاچ‌بک", "سایپا", false, null, true, 82, "کوییک", null, "اتوماتیک" },
                    { 7, "[\"dena\",\"\\u062F\\u0646\\u0627 \\u067E\\u0644\\u0627\\u0633\"]", "سدان", "ایران‌خودرو", false, null, true, 84, "دنا", null, "پلاس توربو" },
                    { 8, "[\"tara\",\"\\u062A\\u0627\\u0631\\u0627\"]", "سدان", "ایران‌خودرو", false, null, true, 80, "تارا", null, "اتوماتیک" },
                    { 9, "[\"samand\",\"\\u0633\\u0645\\u0646\\u062F \\u0633\\u0648\\u0631\\u0646\"]", "سدان", "ایران‌خودرو", false, null, true, 76, "سمند", null, "سورن پلاس" },
                    { 10, "[\"mvm x55\",\"\\u0627\\u0645 \\u0648\\u06CC \\u0627\\u0645 \\u0627\\u06CC\\u06A9\\u063355\"]", "شاسی‌بلند", "ام‌وی‌ام", false, null, true, 70, "X55", null, "پرو" },
                    { 11, "[\"changan\",\"univ\",\"\\u06CC\\u0648\\u0646\\u06CC \\u0648\\u06CC\"]", "شاسی‌بلند", "چانگان", false, null, true, 68, "یونی‌وی", null, null },
                    { 12, "[\"cerato\",\"\\u06A9\\u06CC\\u0627 \\u0633\\u0631\\u0627\\u062A\\u0648\"]", "سدان", "کیا", false, null, true, 66, "سراتو", null, "۲۰۰۰" },
                    { 13, "[\"sonata\",\"\\u0633\\u0648\\u0646\\u0627\\u062A\\u0627\"]", "سدان", "هیوندای", false, null, true, 58, "سوناتا", null, "LF" },
                    { 14, "[\"bmw 730\",\"730li\"]", "سدان", "بی‌ام‌و", true, null, true, 45, "۷۳۰Li", null, "ام‌پکیج" },
                    { 15, "[\"benz e250\",\"e250\"]", "سدان", "بنز", true, null, true, 42, "E250", null, null },
                    { 16, "[\"prado\",\"\\u0644\\u0646\\u062F\\u06A9\\u0631\\u0648\\u0632\\u0631\"]", "شاسی‌بلند", "تویوتا", true, null, true, 55, "لندکروزر", null, "پرادو" }
                });

            migrationBuilder.InsertData(
                table: "PlanLimits",
                columns: new[] { "PlanId", "AllowBellAlerts", "BellAlertDurationHours", "MaxActiveBells", "MonthlyAppraisalLimit", "PrioritySupport", "RealtimeHunts", "TrialAllowed", "TrialHours", "UnlimitedAppraisals" },
                values: new object[] { 1, false, 24, 0, null, false, true, true, 24, true });

            migrationBuilder.InsertData(
                table: "PlanLimits",
                columns: new[] { "PlanId", "AllowBellAlerts", "BellAlertDurationHours", "MaxActiveBells", "MonthlyAppraisalLimit", "PrioritySupport", "RealtimeHunts", "TrialAllowed", "TrialHours", "UnlimitedAppraisals" },
                values: new object[] { 2, true, 24, null, null, true, true, false, 24, true });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "PlanLimits",
                keyColumn: "PlanId",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "PlanLimits",
                keyColumn: "PlanId",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Key",
                keyValue: "BellAlertHours");

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Key",
                keyValue: "CardTransfer.BankName");

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Key",
                keyValue: "CardTransfer.CardNumber");

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Key",
                keyValue: "CardTransfer.HolderName");

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Key",
                keyValue: "CityCapacity");

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Key",
                keyValue: "FreeAppraisalsPerMonthGuest");

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Key",
                keyValue: "GoldenScoreThreshold");

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Key",
                keyValue: "MaxAdsPerMonthBeforeReview");

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Key",
                keyValue: "SupportEndHour");

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Key",
                keyValue: "SupportStartHour");

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Key",
                keyValue: "TransactionCostFloorToman");

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Key",
                keyValue: "TransactionCostRate");

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Key",
                keyValue: "TrialHours");

            migrationBuilder.DeleteData(
                table: "VehicleModels",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "VehicleModels",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "VehicleModels",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "VehicleModels",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "VehicleModels",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "VehicleModels",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "VehicleModels",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "VehicleModels",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "VehicleModels",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "VehicleModels",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "VehicleModels",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "VehicleModels",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "VehicleModels",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "VehicleModels",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "VehicleModels",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "VehicleModels",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "Plans",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Plans",
                keyColumn: "Id",
                keyValue: 2);
        }
    }
}
