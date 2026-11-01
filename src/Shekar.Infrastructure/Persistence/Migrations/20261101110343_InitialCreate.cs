using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shekar.Infrastructure.Persistence.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Advertisers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Phone = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false, comment: "شماره موبایل آگهی‌دهنده؛ یکتا"),
                    DisplayName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true, comment: "نام/مستعار آگهی‌دهنده"),
                    FirstSeenAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, comment: "اولین آگهی دیده‌شده از این شماره"),
                    LastAdAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true, comment: "آخرین آگهی ثبت‌شده"),
                    TotalAdsPosted = table.Column<int>(type: "int", nullable: false, comment: "مجموع کل آگهی‌های این شماره"),
                    AdsPostedLast30Days = table.Column<int>(type: "int", nullable: false, comment: "تعداد آگهی در ۳۰ روز اخیر (تشخیص آگهی بیش از حد)"),
                    IsTrusted = table.Column<bool>(type: "bit", nullable: false, comment: "آگهی‌دهنده‌ی معتبر (از فیلترها مستثنا)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Advertisers", x => x.Id);
                },
                comment: "آگهی‌دهنده‌ها و حجم آگهی‌گذاری‌شان؛ مبنای تشخیص دلال/اسپمر");

            migrationBuilder.CreateTable(
                name: "BlockedAdvertisers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Phone = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false, comment: "شماره موبایل بلاک‌شده"),
                    Reason = table.Column<byte>(type: "tinyint", nullable: false, comment: "دلیل: 1=آگهی‌زیاد،2=دلال،3=اطلاعات‌نادرست،4=تکراری،5=گزارش‌کاربر،6=دستی"),
                    Note = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true, comment: "توضیح تکمیلی"),
                    BlockedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, comment: "زمان بلاک"),
                    BlockedByUserId = table.Column<int>(type: "int", nullable: true, comment: "ادمین بلاک‌کننده؛ NULL=سیستمی/خودکار"),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true, comment: "انقضای بلاک؛ NULL=دائمی"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, comment: "آیا بلاک هم‌اکنون فعال است")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlockedAdvertisers", x => x.Id);
                },
                comment: "شماره‌های بلاک‌شده؛ آگهی‌هایشان از پیشنهادهای روزانه حذف می‌شود (ضد دلال/اسپم/دروغ)");

            migrationBuilder.CreateTable(
                name: "Cities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false, comment: "نام نمایشی شهر، مثل «تهران»"),
                    Slug = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false, comment: "اسلاگ انگلیسی برای مسیرها، مثل «tehran»"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, comment: "آیا شهر فعال است یا «به‌زودی»"),
                    Capacity = table.Column<int>(type: "int", nullable: false, comment: "ظرفیت اشتراک فعال شهر (پیش‌فرض ۱۰۰ طبق قانون R1)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cities", x => x.Id);
                },
                comment: "شهرهای تحت پوشش سرویس");

            migrationBuilder.CreateTable(
                name: "CrawlRuns",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Source = table.Column<byte>(type: "tinyint", nullable: false, comment: "منبع کراول"),
                    CityId = table.Column<int>(type: "int", nullable: true, comment: "شهر هدف؛ NULL=همه"),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, comment: "زمان شروع"),
                    FinishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true, comment: "زمان پایان"),
                    Status = table.Column<byte>(type: "tinyint", nullable: false, comment: "وضعیت: 1=درحال‌اجرا،2=موفق،3=خطا"),
                    FetchedCount = table.Column<int>(type: "int", nullable: false, comment: "تعداد دریافت‌شده"),
                    SavedCount = table.Column<int>(type: "int", nullable: false, comment: "تعداد تازه‌ی ذخیره‌شده"),
                    SkippedCount = table.Column<int>(type: "int", nullable: false, comment: "تعداد ردشده"),
                    RemovedCount = table.Column<int>(type: "int", nullable: false, comment: "تعداد حذف‌شده‌ی تشخیص‌داده‌شده"),
                    ErrorCount = table.Column<int>(type: "int", nullable: false, comment: "تعداد خطا"),
                    Error = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "پیام خطا")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrawlRuns", x => x.Id);
                },
                comment: "لاگ هر پاس کراول برای پایش و عیب‌یابی");

            migrationBuilder.CreateTable(
                name: "OtpCodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Mobile = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false, comment: "شماره موبایل مقصد"),
                    Code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, comment: "کد ۵ رقمی (در Mock ثابت ۱۲۳۴۵)"),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, comment: "زمان تولید"),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, comment: "زمان انقضا"),
                    Consumed = table.Column<bool>(type: "bit", nullable: false, comment: "آیا قبلاً استفاده/تأیید شده")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OtpCodes", x => x.Id);
                },
                comment: "کدهای یک‌بارمصرف ورود");

            migrationBuilder.CreateTable(
                name: "Plans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<byte>(type: "tinyint", nullable: false, comment: "نوع پلن: 1=شکارچی، 2=شکارچی حرفه‌ای"),
                    Name = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false, comment: "نام نمایشی پلن"),
                    MonthlyPriceToman = table.Column<long>(type: "bigint", nullable: false, comment: "قیمت ماهانه به تومان")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plans", x => x.Id);
                },
                comment: "پلن‌های اشتراک (فقط پرداخت ماهانه — R2)");

            migrationBuilder.CreateTable(
                name: "SystemSettings",
                columns: table => new
                {
                    Key = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false, comment: "کلید یکتا، مثل «GoldenScoreThreshold»"),
                    Value = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "مقدار (طبق ValueType تفسیر می‌شود)"),
                    ValueType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, comment: "نوع مقدار: int|decimal|bool|time|string"),
                    Description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true, comment: "توضیح کاربردی تنظیم"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, comment: "زمان آخرین تغییر")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.Key);
                },
                comment: "محدودیت‌ها و تنظیمات سراسری کسب‌وکار (کلید/مقدار، قابل تغییر بدون انتشار)");

            migrationBuilder.CreateTable(
                name: "VehicleModels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Brand = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false, comment: "برند، مثل «پژو»"),
                    Model = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false, comment: "مدل، مثل «۲۰۷»"),
                    Trim = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true, comment: "تریم/تیپ، مثل «دستی پانوراما»"),
                    BodyType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true, comment: "نوع بدنه: هاچ‌بک/سدان/شاسی‌بلند"),
                    LiquidityScore = table.Column<int>(type: "int", nullable: false, comment: "نقدشوندگی از پیش‌تعیین‌شده ۰..۱۰۰ (سرعت فروش)"),
                    Aliases = table.Column<string>(type: "nvarchar(max)", nullable: false, comment: "نام‌های مستعار برای تطبیق متن آگهی (JSON)"),
                    ColorSensitive = table.Column<bool>(type: "bit", nullable: false, comment: "آیا رنگ برای این مدل همیشه در قیمت مهم است"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, comment: "آیا مدل فعال است"),
                    Notes = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true, comment: "یادداشت")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleModels", x => x.Id);
                },
                comment: "کاتالوگ خودرو + نقدشوندگیِ دستی هر مدل؛ مرجع نرمال‌سازی و سگمنت‌بندی");

            migrationBuilder.CreateTable(
                name: "Hunts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "عنوان آگهی"),
                    ModelYear = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, comment: "سال مدل (شمسی/میلادی طبق آگهی)"),
                    MileageKm = table.Column<int>(type: "int", nullable: true, comment: "کارکرد به کیلومتر؛ NULL برای صفر"),
                    Location = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false, comment: "محله/منطقه‌ی آگهی"),
                    CityId = table.Column<int>(type: "int", nullable: false, comment: "شهر آگهی"),
                    PriceToman = table.Column<long>(type: "bigint", nullable: false, comment: "قیمت آگهی به تومان"),
                    MarketMedianToman = table.Column<long>(type: "bigint", nullable: false, comment: "میانه‌ی قیمت بازار به تومان (R9)"),
                    BelowMarketPercent = table.Column<int>(type: "int", nullable: false, comment: "درصد زیر بازار"),
                    Score = table.Column<int>(type: "int", nullable: false, comment: "امتیاز شکار ۰ تا ۱۰۰ (≥۸۵ فرصت طلایی — R8)"),
                    Liquidity = table.Column<byte>(type: "tinyint", nullable: false, comment: "نقدشوندگی: 1=متوسط، 2=بالا"),
                    State = table.Column<byte>(type: "tinyint", nullable: false, comment: "وضعیت: 1=فعال، 2=حذف‌شده (R7)"),
                    PublishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, comment: "زمان انتشار آگهی"),
                    IsNew = table.Column<bool>(type: "bit", nullable: false, comment: "نمایش برچسب «تازه»"),
                    ImageFileName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true, comment: "نام فایل تصویر؛ NULL=placeholder"),
                    AdvertiserPhone = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true, comment: "شماره آگهی‌دهنده؛ برای حذف شماره‌های بلاک‌شده از پیشنهادها"),
                    SourceListingId = table.Column<long>(type: "bigint", nullable: true, comment: "آگهی خامِ منبعِ این شکار (خروجی اسکورینگ)؛ NULL برای seed/دستی")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hunts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Hunts_Cities_CityId",
                        column: x => x.CityId,
                        principalTable: "Cities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "شکارها: آگهی‌های زیر قیمت بازار با نقدشوندگی بالا (پیشنهادهای روزانه)");

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Mobile = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false, comment: "شماره موبایل؛ شناسه‌ی یکتای ورود"),
                    FirstName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "نام"),
                    LastName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "نام خانوادگی"),
                    CityId = table.Column<int>(type: "int", nullable: true, comment: "شهر انتخاب‌شده در پروفایل"),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, comment: "زمان ثبت‌نام"),
                    HasUsedTrial = table.Column<bool>(type: "bit", nullable: false, comment: "R3 — آیا پیش‌تر از ۲۴ ساعت رایگان استفاده کرده"),
                    ProfileCompleted = table.Column<bool>(type: "bit", nullable: false, comment: "آیا پروفایل (ورود اول) تکمیل شده")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Cities_CityId",
                        column: x => x.CityId,
                        principalTable: "Cities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                },
                comment: "کاربران ثبت‌نام‌شده (ورود با موبایل + OTP)");

            migrationBuilder.CreateTable(
                name: "WaitlistEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CityId = table.Column<int>(type: "int", nullable: false, comment: "شهر لیست انتظار"),
                    Mobile = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false, comment: "شماره موبایل متقاضی"),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, comment: "زمان ثبت در صف"),
                    Position = table.Column<int>(type: "int", nullable: false, comment: "نوبت نمایش‌داده‌شده در صف")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WaitlistEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WaitlistEntries_Cities_CityId",
                        column: x => x.CityId,
                        principalTable: "Cities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "لیست انتظار شهرِ پرظرفیت (R1)");

            migrationBuilder.CreateTable(
                name: "PlanLimits",
                columns: table => new
                {
                    PlanId = table.Column<int>(type: "int", nullable: false, comment: "کلید اصلی و کلید خارجی به Plans"),
                    UnlimitedAppraisals = table.Column<bool>(type: "bit", nullable: false, comment: "آیا کارشناسی نامحدود است (مشترک‌ها)"),
                    MonthlyAppraisalLimit = table.Column<int>(type: "int", nullable: true, comment: "سقف کارشناسی ماهانه وقتی نامحدود نیست؛ NULL=نامحدود"),
                    AllowBellAlerts = table.Column<bool>(type: "bit", nullable: false, comment: "R6 — فعال‌بودن «خبرم کن» (فقط پلن حرفه‌ای)"),
                    BellAlertDurationHours = table.Column<int>(type: "int", nullable: false, comment: "مدت فعال‌بودن هر زنگوله به ساعت (۲۴)"),
                    MaxActiveBells = table.Column<int>(type: "int", nullable: true, comment: "بیشینه‌ی زنگوله‌های فعال هم‌زمان؛ NULL=نامحدود"),
                    TrialAllowed = table.Column<bool>(type: "bit", nullable: false, comment: "R3 — داشتن اشتراک آزمایشی رایگان"),
                    TrialHours = table.Column<int>(type: "int", nullable: false, comment: "مدت اشتراک آزمایشی به ساعت (۲۴)"),
                    RealtimeHunts = table.Column<bool>(type: "bit", nullable: false, comment: "نمایش شکارها در لحظه"),
                    PrioritySupport = table.Column<bool>(type: "bit", nullable: false, comment: "پشتیبانی اولویت‌دار")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanLimits", x => x.PlanId);
                    table.ForeignKey(
                        name: "FK_PlanLimits_Plans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "Plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "محدودیت‌ها و امکانات هر پلن (داده‌محور کردن قوانین پلن)");

            migrationBuilder.CreateTable(
                name: "CarListings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Source = table.Column<byte>(type: "tinyint", nullable: false, comment: "منبع: 1=دیوار، 2=باما"),
                    ExternalId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false, comment: "شناسه‌ی آگهی در منبع (یکتا با Source)"),
                    Url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false, comment: "آدرس آگهی در منبع"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "عنوان آگهی"),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "توضیحات آگهی"),
                    VehicleModelId = table.Column<int>(type: "int", nullable: true, comment: "مدل خودرو پس از نرمال‌سازی؛ NULL تا تطبیق"),
                    RawBrand = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true, comment: "برند خام"),
                    RawModel = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true, comment: "مدل خام"),
                    RawTrim = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true, comment: "تریم خام"),
                    ModelYear = table.Column<int>(type: "int", nullable: true, comment: "سال مدل"),
                    MileageKm = table.Column<int>(type: "int", nullable: true, comment: "کارکرد (کیلومتر)؛ NULL=صفر/نامشخص"),
                    Color = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true, comment: "رنگ خودرو"),
                    BodyCondition = table.Column<byte>(type: "tinyint", nullable: false, comment: "وضعیت بدنه: 0=نامشخص،1=سالم،2=رنگ،3=تعویض،4=تصادفی"),
                    PriceToman = table.Column<long>(type: "bigint", nullable: false, comment: "قیمت آگهی (تومان)"),
                    CityId = table.Column<int>(type: "int", nullable: true, comment: "شهر نگاشت‌شده؛ NULL=نامشخص"),
                    RawLocation = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true, comment: "شهر/محله‌ی خام"),
                    AdvertiserPhone = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true, comment: "شماره آگهی‌دهنده (فیلتر بلاک)"),
                    ImageCount = table.Column<int>(type: "int", nullable: false, comment: "تعداد عکس (شرط ذخیره: >۰)"),
                    FirstImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true, comment: "آدرس اولین عکس"),
                    PublishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, comment: "زمان انتشار در منبع (فیلتر ۲۴ ساعت)"),
                    FetchedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, comment: "زمان اولین دریافت"),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, comment: "آخرین دیده‌شدن در کراول (تشخیص حذف)"),
                    State = table.Column<byte>(type: "tinyint", nullable: false, comment: "وضعیت: 1=فعال، 2=حذف‌شده"),
                    RemovedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true, comment: "زمان تشخیص حذف"),
                    IsScored = table.Column<bool>(type: "bit", nullable: false, comment: "آیا اسکورینگ پردازش کرده"),
                    ContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true, comment: "اثرانگشت محتوا (تشخیص تکرار نزدیک)"),
                    RawJson = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "بدنه‌ی خام آگهی (JSON)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarListings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CarListings_Cities_CityId",
                        column: x => x.CityId,
                        principalTable: "Cities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CarListings_VehicleModels_VehicleModelId",
                        column: x => x.VehicleModelId,
                        principalTable: "VehicleModels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                },
                comment: "آگهی‌های خام کراول‌شده (دیوار/باما) پیش از اسکورینگ");

            migrationBuilder.CreateTable(
                name: "MarketSegmentStats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SegmentKey = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false, comment: "کلید یکتای سگمنت (upsert)"),
                    VehicleModelId = table.Column<int>(type: "int", nullable: false, comment: "مدل خودرو"),
                    ModelYear = table.Column<int>(type: "int", nullable: false, comment: "سال مدل"),
                    MileageBucket = table.Column<byte>(type: "tinyint", nullable: false, comment: "باکت کارکرد: 0=صفر..5=+۱۵۰هزار"),
                    BodyCondition = table.Column<byte>(type: "tinyint", nullable: false, comment: "وضعیت بدنه"),
                    ColorKey = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true, comment: "رنگ؛ NULL=در این سگمنت دخیل نیست"),
                    SampleCount = table.Column<int>(type: "int", nullable: false, comment: "تعداد نمونه در پنجره"),
                    MedianPriceToman = table.Column<long>(type: "bigint", nullable: false, comment: "میانه‌ی قیمت (مرجع اصلی)"),
                    AveragePriceToman = table.Column<long>(type: "bigint", nullable: false, comment: "میانگین قیمت"),
                    P25PriceToman = table.Column<long>(type: "bigint", nullable: false, comment: "چارک اول قیمت"),
                    P75PriceToman = table.Column<long>(type: "bigint", nullable: false, comment: "چارک سوم قیمت"),
                    WindowDays = table.Column<int>(type: "int", nullable: false, comment: "طول پنجره‌ی نمونه‌گیری (روز)"),
                    ComputedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, comment: "زمان آخرین محاسبه")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketSegmentStats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketSegmentStats_VehicleModels_VehicleModelId",
                        column: x => x.VehicleModelId,
                        principalTable: "VehicleModels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "آمار ساعتیِ هر سگمنت بازار؛ مرجع تشخیص زیرقیمت‌بودن");

            migrationBuilder.CreateTable(
                name: "HuntAnalyses",
                columns: table => new
                {
                    HuntId = table.Column<int>(type: "int", nullable: false, comment: "کلید اصلی و کلید خارجی به Hunts"),
                    ComparableCount = table.Column<int>(type: "int", nullable: false, comment: "تعداد آگهی‌های مشابه مبنای تحلیل"),
                    Confidence = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, comment: "سطح اطمینان نمایشی، مثل «بالا»"),
                    FairRangeMinToman = table.Column<long>(type: "bigint", nullable: false, comment: "کف بازه‌ی منصفانه (تومان)"),
                    FairRangeMaxToman = table.Column<long>(type: "bigint", nullable: false, comment: "سقف بازه‌ی منصفانه (تومان)"),
                    Summary = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: false, comment: "خلاصه‌ی متنی هوش مصنوعی"),
                    Risks = table.Column<string>(type: "nvarchar(max)", nullable: false, comment: "ریسک‌ها و نکته‌های پیش از خرید (JSON)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HuntAnalyses", x => x.HuntId);
                    table.ForeignKey(
                        name: "FK_HuntAnalyses_Hunts_HuntId",
                        column: x => x.HuntId,
                        principalTable: "Hunts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "تحلیل تفصیلی هر شکار (یک‌به‌یک با Hunts) برای صفحه‌ی جزئیات/کارشناسی");

            migrationBuilder.CreateTable(
                name: "Appraisals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false, comment: "کاربر انجام‌دهنده"),
                    AdUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false, comment: "لینک آگهی کارشناسی‌شده"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "عنوان خودرو"),
                    Score = table.Column<int>(type: "int", nullable: false, comment: "امتیاز نتیجه ۰ تا ۱۰۰"),
                    Verdict = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false, comment: "برچسب نتیجه: «فرصت طلایی»/«منصفانه»/«گران»"),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, comment: "زمان انجام کارشناسی (مبنای شمارش سهمیه‌ی ماهانه)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Appraisals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Appraisals_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "کارشناسی‌های قیمت کاربران (R5)");

            migrationBuilder.CreateTable(
                name: "BellAlerts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false, comment: "کاربر صاحب زنگوله"),
                    Category = table.Column<byte>(type: "tinyint", nullable: false, comment: "دسته‌ی قیمتی (۱ تا ۸)"),
                    ActivatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true, comment: "زمان روشن‌شدن؛ NULL=خاموش"),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true, comment: "زمان خاموش‌شدن خودکار (ActivatedAt + ۲۴ ساعت)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BellAlerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BellAlerts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "زنگوله‌های «خبرم کن» هر کاربر برای دسته‌های قیمتی (R6)");

            migrationBuilder.CreateTable(
                name: "Subscriptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false, comment: "کاربر صاحب اشتراک"),
                    CityId = table.Column<int>(type: "int", nullable: false, comment: "شهر اشتراک"),
                    PlanType = table.Column<byte>(type: "tinyint", nullable: false, comment: "نوع پلن"),
                    Status = table.Column<byte>(type: "tinyint", nullable: false, comment: "وضعیت: 1=Trial، 2=Active، 3=Expired"),
                    StartsAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, comment: "شروع اشتراک"),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, comment: "پایان اعتبار (Trial: +۲۴ ساعت، Active: ماهانه)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Subscriptions_Cities_CityId",
                        column: x => x.CityId,
                        principalTable: "Cities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Subscriptions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "اشتراک‌های کاربران؛ Trial+Active در ظرفیت شهر شمرده می‌شوند (R1)");

            migrationBuilder.CreateTable(
                name: "HuntAnalysisFactors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false, comment: "کلید اصلی")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false, comment: "نام سنجه"),
                    Rating = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, comment: "برچسب کیفی، مثل «عالی»"),
                    Detail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "توضیح سنجه"),
                    HuntId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HuntAnalysisFactors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HuntAnalysisFactors_HuntAnalyses_HuntId",
                        column: x => x.HuntId,
                        principalTable: "HuntAnalyses",
                        principalColumn: "HuntId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Advertisers_Phone",
                table: "Advertisers",
                column: "Phone",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Appraisals_UserId_CreatedAt",
                table: "Appraisals",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BellAlerts_UserId_Category",
                table: "BellAlerts",
                columns: new[] { "UserId", "Category" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BlockedAdvertisers_Phone_IsActive",
                table: "BlockedAdvertisers",
                columns: new[] { "Phone", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_CarListings_AdvertiserPhone",
                table: "CarListings",
                column: "AdvertiserPhone");

            migrationBuilder.CreateIndex(
                name: "IX_CarListings_CityId_PublishedAt_State",
                table: "CarListings",
                columns: new[] { "CityId", "PublishedAt", "State" });

            migrationBuilder.CreateIndex(
                name: "IX_CarListings_Source_ExternalId",
                table: "CarListings",
                columns: new[] { "Source", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CarListings_State_IsScored",
                table: "CarListings",
                columns: new[] { "State", "IsScored" });

            migrationBuilder.CreateIndex(
                name: "IX_CarListings_VehicleModelId",
                table: "CarListings",
                column: "VehicleModelId");

            migrationBuilder.CreateIndex(
                name: "IX_Cities_Slug",
                table: "Cities",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CrawlRuns_Source_StartedAt",
                table: "CrawlRuns",
                columns: new[] { "Source", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_HuntAnalysisFactors_HuntId",
                table: "HuntAnalysisFactors",
                column: "HuntId");

            migrationBuilder.CreateIndex(
                name: "IX_Hunts_AdvertiserPhone",
                table: "Hunts",
                column: "AdvertiserPhone");

            migrationBuilder.CreateIndex(
                name: "IX_Hunts_CityId_PublishedAt_State",
                table: "Hunts",
                columns: new[] { "CityId", "PublishedAt", "State" });

            migrationBuilder.CreateIndex(
                name: "IX_Hunts_Score",
                table: "Hunts",
                column: "Score");

            migrationBuilder.CreateIndex(
                name: "IX_Hunts_SourceListingId",
                table: "Hunts",
                column: "SourceListingId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketSegmentStats_SegmentKey",
                table: "MarketSegmentStats",
                column: "SegmentKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MarketSegmentStats_VehicleModelId_ModelYear_MileageBucket_BodyCondition",
                table: "MarketSegmentStats",
                columns: new[] { "VehicleModelId", "ModelYear", "MileageBucket", "BodyCondition" });

            migrationBuilder.CreateIndex(
                name: "IX_OtpCodes_Mobile_CreatedAt",
                table: "OtpCodes",
                columns: new[] { "Mobile", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Plans_Type",
                table: "Plans",
                column: "Type",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_CityId_Status_ExpiresAt",
                table: "Subscriptions",
                columns: new[] { "CityId", "Status", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_UserId_ExpiresAt",
                table: "Subscriptions",
                columns: new[] { "UserId", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_CityId",
                table: "Users",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Mobile",
                table: "Users",
                column: "Mobile",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleModels_Brand_Model_Trim",
                table: "VehicleModels",
                columns: new[] { "Brand", "Model", "Trim" });

            migrationBuilder.CreateIndex(
                name: "IX_WaitlistEntries_CityId_Mobile",
                table: "WaitlistEntries",
                columns: new[] { "CityId", "Mobile" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Advertisers");

            migrationBuilder.DropTable(
                name: "Appraisals");

            migrationBuilder.DropTable(
                name: "BellAlerts");

            migrationBuilder.DropTable(
                name: "BlockedAdvertisers");

            migrationBuilder.DropTable(
                name: "CarListings");

            migrationBuilder.DropTable(
                name: "CrawlRuns");

            migrationBuilder.DropTable(
                name: "HuntAnalysisFactors");

            migrationBuilder.DropTable(
                name: "MarketSegmentStats");

            migrationBuilder.DropTable(
                name: "OtpCodes");

            migrationBuilder.DropTable(
                name: "PlanLimits");

            migrationBuilder.DropTable(
                name: "Subscriptions");

            migrationBuilder.DropTable(
                name: "SystemSettings");

            migrationBuilder.DropTable(
                name: "WaitlistEntries");

            migrationBuilder.DropTable(
                name: "HuntAnalyses");

            migrationBuilder.DropTable(
                name: "VehicleModels");

            migrationBuilder.DropTable(
                name: "Plans");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Hunts");

            migrationBuilder.DropTable(
                name: "Cities");
        }
    }
}
