using Shekar.Domain.Entities;
using Shekar.Domain.Enums;

namespace Shekar.Infrastructure.Persistence;

/// <summary>
/// داده‌ی اولیه‌ی مرجع (seed) که مستقیم از طریقِ مایگریشن (HasData) در دیتابیس درج می‌شود،
/// تا تنها با <c>dotnet ef database update</c> یک دیتابیسِ آماده‌ی کار ساخته شود.
/// مقادیر عیناً با <c>database/schema.sql</c> هماهنگ‌اند تا دو مسیرِ ساختِ دیتابیس یکی بمانند.
/// شناسه‌ها ثابت‌اند؛ رجیسترِ خودکارِ کراولر از شناسه‌های بعدی ادامه می‌دهد.
/// </summary>
internal static class SeedData
{
    /// <summary>زمانِ ثابتِ seed (HasData به مقدارِ ثابت نیاز دارد؛ نباید DateTimeOffset.Now باشد).</summary>
    public static readonly DateTimeOffset SeededAt = new(2026, 11, 17, 0, 0, 0, TimeSpan.Zero);

    public static City[] Cities() =>
    [
        new City { Id = 1, Name = "تهران",  Slug = "tehran",  IsActive = true,  Capacity = 100 },
        new City { Id = 2, Name = "کرج",    Slug = "karaj",   IsActive = false, Capacity = 100 },
        new City { Id = 3, Name = "اصفهان", Slug = "isfahan", IsActive = false, Capacity = 100 },
        new City { Id = 4, Name = "مشهد",   Slug = "mashhad", IsActive = false, Capacity = 100 },
    ];

    public static Plan[] Plans() =>
    [
        new Plan { Id = 1, Type = PlanType.Hunter,    Name = "شکارچی",         MonthlyPriceToman = 990_000 },
        new Plan { Id = 2, Type = PlanType.HunterPro, Name = "شکارچی حرفه‌ای", MonthlyPriceToman = 1_499_000 },
    ];

    public static PlanLimit[] PlanLimits() =>
    [
        // شکارچی: کارشناسی نامحدود، ۲۴ ساعت رایگان دارد، بدون «خبرم کن»
        new PlanLimit
        {
            PlanId = 1, UnlimitedAppraisals = true, MonthlyAppraisalLimit = null,
            AllowBellAlerts = false, BellAlertDurationHours = 24, MaxActiveBells = 0,
            TrialAllowed = true, TrialHours = 24, RealtimeHunts = true, PrioritySupport = false,
        },
        // حرفه‌ای: «خبرم کن» + اولویت پشتیبانی، بدون ۲۴ ساعت رایگان
        new PlanLimit
        {
            PlanId = 2, UnlimitedAppraisals = true, MonthlyAppraisalLimit = null,
            AllowBellAlerts = true, BellAlertDurationHours = 24, MaxActiveBells = null,
            TrialAllowed = false, TrialHours = 24, RealtimeHunts = true, PrioritySupport = true,
        },
    ];

    public static SystemSetting[] SystemSettings() =>
    [
        S("CityCapacity",               "100",      "int",     "R1 — ظرفیت پیش‌فرض اشتراک فعال هر شهر"),
        S("FreeAppraisalsPerMonthGuest","3",        "int",     "R5 — سهمیه‌ی کارشناسی رایگان ماهانه برای کاربر بدون اشتراک"),
        S("TrialHours",                 "24",       "int",     "R3 — مدت اشتراک آزمایشی رایگان (ساعت)"),
        S("BellAlertHours",             "24",       "int",     "R6 — مدت فعال‌بودن زنگوله پیش از خاموشی خودکار (ساعت)"),
        S("GoldenScoreThreshold",       "85",       "int",     "R8 — حداقل امتیاز برای «فرصت طلایی»"),
        S("TransactionCostFloorToman",  "15000000", "int",     "R9 — کف هزینه‌ی نقل‌وانتقال و فروش (تومان)"),
        S("TransactionCostRate",        "0.02",     "decimal", "R9 — نرخ هزینه‌ی نقل‌وانتقال نسبت به قیمت آگهی"),
        S("SupportStartHour",           "9",        "int",     "R12 — ساعت شروع پشتیبانی (Asia/Tehran)"),
        S("SupportEndHour",             "18",       "int",     "R12 — ساعت پایان پشتیبانی (Asia/Tehran)"),
        S("MaxAdsPerMonthBeforeReview", "20",       "int",     "آستانه‌ی تعداد آگهی ماهانه برای بررسی دلال/اسپم"),
        S("CardTransfer.CardNumber",    "6219861082608372", "string", "شماره کارت مقصد پرداخت کارت‌به‌کارت"),
        S("CardTransfer.HolderName",    "علیرضا باقری",     "string", "نام صاحب کارت مقصد"),
        S("CardTransfer.BankName",      "بانک سامان",       "string", "نام بانک کارت مقصد"),
    ];

    private static SystemSetting S(string key, string value, string type, string description) =>
        new() { Key = key, Value = value, ValueType = type, Description = description, UpdatedAt = SeededAt };

    public static VehicleModel[] VehicleModels() =>
    [
        V(1,  "پژو",         "۲۰۷",      "دستی",       "هاچ‌بک",    90, ["207", "پژو 207", "peugeot 207"]),
        V(2,  "پژو",         "۲۰۶",      "تیپ ۵",      "هاچ‌بک",    85, ["206", "پژو 206"]),
        V(3,  "پژو",         "پارس",     null,         "سدان",      80, ["pars", "پژو پارس"]),
        V(4,  "سایپا",       "پراید",    "۱۱۱",        "هاچ‌بک",    88, ["pride", "پراید 111"]),
        V(5,  "سایپا",       "تیبا",     null,         "سدان",      78, ["tiba", "تیبا"]),
        V(6,  "سایپا",       "کوییک",    "اتوماتیک",   "هاچ‌بک",    82, ["quick", "کوییک کراس"]),
        V(7,  "ایران‌خودرو", "دنا",      "پلاس توربو", "سدان",      84, ["dena", "دنا پلاس"]),
        V(8,  "ایران‌خودرو", "تارا",     "اتوماتیک",   "سدان",      80, ["tara", "تارا"]),
        V(9,  "ایران‌خودرو", "سمند",     "سورن پلاس",  "سدان",      76, ["samand", "سمند سورن"]),
        V(10, "ام‌وی‌ام",    "X55",      "پرو",        "شاسی‌بلند", 70, ["mvm x55", "ام وی ام ایکس55"]),
        V(11, "چانگان",      "یونی‌وی",  null,         "شاسی‌بلند", 68, ["changan", "univ", "یونی وی"]),
        V(12, "کیا",         "سراتو",    "۲۰۰۰",       "سدان",      66, ["cerato", "کیا سراتو"]),
        V(13, "هیوندای",     "سوناتا",   "LF",         "سدان",      58, ["sonata", "سوناتا"]),
        V(14, "بی‌ام‌و",      "۷۳۰Li",    "ام‌پکیج",    "سدان",      45, ["bmw 730", "730li"],   colorSensitive: true),
        V(15, "بنز",         "E250",     null,         "سدان",      42, ["benz e250", "e250"],  colorSensitive: true),
        V(16, "تویوتا",      "لندکروزر", "پرادو",      "شاسی‌بلند", 55, ["prado", "لندکروزر"],  colorSensitive: true),
    ];

    private static VehicleModel V(
        int id, string brand, string model, string? trim, string bodyType,
        int liquidity, string[] aliases, bool colorSensitive = false) =>
        new()
        {
            Id = id, Brand = brand, Model = model, Trim = trim, DivarKey = null,
            BodyType = bodyType, LiquidityScore = liquidity, Aliases = aliases,
            ColorSensitive = colorSensitive, IsActive = true, Notes = null,
        };
}
