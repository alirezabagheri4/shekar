using Shekar.Application.Abstractions;

using Shekar.Domain;
using Shekar.Domain.Entities;
using Shekar.Domain.Enums;

namespace Shekar.Mock;

/// <summary>
/// پایگاه‌داده‌ی درون‌حافظه‌ای و singleton برای داده‌های ماک.
/// داده‌ها عیناً از صفحات نمونه (لیست شکارهای دسکتاپ، حساب، جزئیات) برداشته شده‌اند.
/// </summary>
public sealed class MockDataStore
{
    private readonly object _gate = new();

    public List<City> Cities { get; } = new();
    public List<Plan> Plans { get; } = new();
    public List<User> Users { get; } = new();
    public List<Subscription> Subscriptions { get; } = new();
    public List<Hunt> Hunts { get; } = new();
    public List<Appraisal> Appraisals { get; } = new();
    public List<BellAlert> BellAlerts { get; } = new();
    public List<WaitlistEntry> Waitlist { get; } = new();
    public List<OtpCode> Otps { get; } = new();
    public List<PlanLimit> PlanLimits { get; } = new();
    public List<SystemSetting> SystemSettings { get; } = new();
    public List<Advertiser> Advertisers { get; } = new();
    public List<BlockedAdvertiser> BlockedAdvertisers { get; } = new();
    public List<CarListing> CarListings { get; } = new();
    public List<CrawlRun> CrawlRuns { get; } = new();
    public List<VehicleModel> VehicleModels { get; } = new();
    public List<MarketSegmentStat> MarketSegmentStats { get; } = new();

    /// <summary>شناسه‌ی کاربر نمونه‌ی لاگین‌شده (کامران رضایی، مشترک حرفه‌ای).</summary>
    public const int DemoUserId = 1;

    public MockDataStore(IClock clock)
    {
        Seed(clock.UtcNow);
    }

    public int NextId<T>(IEnumerable<T> items, Func<T, int> idSelector)
    {
        lock (_gate)
        {
            return items.Select(idSelector).DefaultIfEmpty(0).Max() + 1;
        }
    }

    public object Gate => _gate;

    private void Seed(DateTimeOffset now)
    {
        Cities.AddRange(new[]
        {
            new City { Id = 1, Name = "تهران", Slug = "tehran", IsActive = true, Capacity = DomainConstants.CityCapacity },
            new City { Id = 2, Name = "کرج", Slug = "karaj", IsActive = false, Capacity = DomainConstants.CityCapacity },
            new City { Id = 3, Name = "اصفهان", Slug = "isfahan", IsActive = false, Capacity = DomainConstants.CityCapacity },
            new City { Id = 4, Name = "مشهد", Slug = "mashhad", IsActive = false, Capacity = DomainConstants.CityCapacity }
        });

        Plans.Add(new Plan
        {
            Id = 1,
            Type = PlanType.Hunter,
            Name = "شکارچی",
            MonthlyPriceToman = 990_000,
            Features = new[]
            {
                "شکارها در لحظه، همه‌ی دسته‌ها",
                "تحلیل کامل هر آگهی",
                "کارشناسی لینک آگهی نامحدود"
            }
        });
        Plans.Add(new Plan
        {
            Id = 2,
            Type = PlanType.HunterPro,
            Name = "شکارچی حرفه‌ای",
            MonthlyPriceToman = 1_499_000,
            Features = new[]
            {
                "همه‌ی امکانات شکارچی",
                "پیامک شکارهای تازه‌ی هر دسته",
                "زنگوله‌ی «خبرم کن» ۲۴ ساعته",
                "اولویت در پشتیبانی"
            }
        });

        Users.Add(new User
        {
            Id = DemoUserId,
            Mobile = "09123456789",
            FirstName = "کامران",
            LastName = "رضایی",
            CityId = 1,
            CreatedAt = now.AddMonths(-2),
            HasUsedTrial = true,
            ProfileCompleted = true
        });

        Subscriptions.Add(new Subscription
        {
            Id = 1,
            UserId = DemoUserId,
            CityId = 1,
            PlanType = PlanType.HunterPro,
            Status = SubscriptionStatus.Active,
            StartsAt = now.AddDays(-1),
            ExpiresAt = now.AddDays(30)
        });

        SeedHunts(now);
        SeedBells(now);
        SeedAppraisals(now);
        SeedModeration(now);
        SeedVehicleModels();

        // هر شکار یک شماره‌ی آگهی‌دهنده دارد تا فیلتر شماره‌های بلاک‌شده قابل‌استفاده باشد.
        foreach (Hunt h in Hunts)
        {
            h.AdvertiserPhone ??= $"09120{h.Id:0000}00";
        }

        // تضمین اینکه همه‌ی شکارهای seed روی «امروزِ تهران» بمانند (مستقل از ساعتِ اجرا).
        DateTimeOffset tehranNow = Services.TehranTime.ToTehran(now);
        DateTimeOffset todayFloorUtc = new DateTimeOffset(tehranNow.Date, tehranNow.Offset).AddMinutes(1).ToUniversalTime();
        foreach (Hunt h in Hunts)
        {
            if (h.PublishedAt < todayFloorUtc)
            {
                h.PublishedAt = todayFloorUtc;
            }
        }
    }

    private void SeedPlanLimits()
    {
        // شکارچی: کارشناسی نامحدود، بدون «خبرم کن»، دارای ۲۴ ساعت رایگان.
        PlanLimits.Add(new PlanLimit
        {
            PlanId = 1,
            UnlimitedAppraisals = true,
            MonthlyAppraisalLimit = null,
            AllowBellAlerts = false,
            BellAlertDurationHours = 24,
            MaxActiveBells = 0,
            TrialAllowed = true,
            TrialHours = 24,
            RealtimeHunts = true,
            PrioritySupport = false
        });
        // حرفه‌ای: همه‌چیز + «خبرم کن» + پشتیبانی اولویت‌دار، بدون ۲۴ ساعت رایگان.
        PlanLimits.Add(new PlanLimit
        {
            PlanId = 2,
            UnlimitedAppraisals = true,
            MonthlyAppraisalLimit = null,
            AllowBellAlerts = true,
            BellAlertDurationHours = 24,
            MaxActiveBells = null,
            TrialAllowed = false,
            TrialHours = 24,
            RealtimeHunts = true,
            PrioritySupport = true
        });
    }

    private void SeedSystemSettings(DateTimeOffset now)
    {
        void Set(string key, string value, string type, string desc) =>
            SystemSettings.Add(new SystemSetting { Key = key, Value = value, ValueType = type, Description = desc, UpdatedAt = now });

        Set("CityCapacity", "100", "int", "R1 — ظرفیت پیش‌فرض اشتراک فعال هر شهر");
        Set("FreeAppraisalsPerMonthGuest", "3", "int", "R5 — سهمیه‌ی کارشناسی رایگان ماهانه برای کاربر بدون اشتراک");
        Set("TrialHours", "24", "int", "R3 — مدت اشتراک آزمایشی رایگان (ساعت)");
        Set("BellAlertHours", "24", "int", "R6 — مدت فعال‌بودن زنگوله پیش از خاموش‌شدن خودکار (ساعت)");
        Set("GoldenScoreThreshold", "85", "int", "R8 — حداقل امتیاز برای «فرصت طلایی»");
        Set("TransactionCostFloorToman", "15000000", "int", "R9 — کف هزینه‌ی نقل‌وانتقال و فروش (تومان)");
        Set("TransactionCostRate", "0.02", "decimal", "R9 — نرخ هزینه‌ی نقل‌وانتقال نسبت به قیمت آگهی");
        Set("SupportStartHour", "9", "int", "R12 — ساعت شروع پشتیبانی (Asia/Tehran)");
        Set("SupportEndHour", "18", "int", "R12 — ساعت پایان پشتیبانی (Asia/Tehran)");
        Set("MaxAdsPerMonthBeforeReview", "20", "int", "آستانه‌ی تعداد آگهی ماهانه برای بررسی دلال/اسپم");
    }

    private void SeedVehicleModels()
    {
        int id = 1;
        void Add(string brand, string model, string? trim, string? body, int liq, string[] aliases, bool colorSensitive = false) =>
            VehicleModels.Add(new VehicleModel
            {
                Id = id++, Brand = brand, Model = model, Trim = trim, BodyType = body,
                LiquidityScore = liq, Aliases = aliases, ColorSensitive = colorSensitive, IsActive = true
            });

        Add("پژو", "۲۰۷", "دستی", "هاچ‌بک", 90, new[] { "207", "پژو 207", "peugeot 207" });
        Add("پژو", "۲۰۶", "تیپ ۵", "هاچ‌بک", 85, new[] { "206", "پژو 206" });
        Add("پژو", "پارس", null, "سدان", 80, new[] { "pars", "پژو پارس" });
        Add("سایپا", "پراید", "۱۱۱", "هاچ‌بک", 88, new[] { "pride", "پراید 111" });
        Add("سایپا", "تیبا", null, "سدان", 78, new[] { "tiba", "تیبا" });
        Add("سایپا", "کوییک", "اتوماتیک", "هاچ‌بک", 82, new[] { "quick", "کوییک کراس" });
        Add("ایران‌خودرو", "دنا", "پلاس توربو", "سدان", 84, new[] { "dena", "دنا پلاس" });
        Add("ایران‌خودرو", "تارا", "اتوماتیک", "سدان", 80, new[] { "tara", "تارا" });
        Add("ایران‌خودرو", "سمند", "سورن پلاس", "سدان", 76, new[] { "samand", "سمند سورن" });
        Add("ام‌وی‌ام", "X55", "پرو", "شاسی‌بلند", 70, new[] { "mvm x55", "ام وی ام ایکس55" });
        Add("چانگان", "یونی‌وی", null, "شاسی‌بلند", 68, new[] { "changan", "univ", "یونی وی" });
        Add("کیا", "سراتو", "۲۰۰۰", "سدان", 66, new[] { "cerato", "کیا سراتو" });
        Add("هیوندای", "سوناتا", "LF", "سدان", 58, new[] { "sonata", "سوناتا" });
        Add("بی‌ام‌و", "۷۳۰Li", "ام‌پکیج", "سدان", 45, new[] { "bmw 730", "730li" }, colorSensitive: true);
        Add("بنز", "E250", null, "سدان", 42, new[] { "benz e250", "e250" }, colorSensitive: true);
        Add("تویوتا", "لندکروزر", "پرادو", "شاسی‌بلند", 55, new[] { "prado", "لندکروزر" }, colorSensitive: true);
    }

    private void SeedModeration(DateTimeOffset now)
    {
        SeedPlanLimits();
        SeedSystemSettings(now);
        // نمونه‌ی آگهی‌دهنده‌ی پرآگهی و یک بلاک نمونه (شماره‌ای که در شکارهای امروز نیست).
        Advertisers.Add(new Advertiser
        {
            Id = 1, Phone = "09129999999", DisplayName = "نمایشگاه ماشین",
            FirstSeenAt = now.AddMonths(-3), LastAdAt = now.AddDays(-1),
            TotalAdsPosted = 180, AdsPostedLast30Days = 47, IsTrusted = false
        });
        BlockedAdvertisers.Add(new BlockedAdvertiser
        {
            Id = 1, Phone = "09129999999", Reason = BlockReason.TooManyAds,
            Note = "۴۷ آگهی در ۳۰ روز اخیر", BlockedAt = now.AddDays(-2),
            BlockedByUserId = null, ExpiresAt = null, IsActive = true
        });
    }

    private void SeedHunts(DateTimeOffset now)
    {
        int id = 1;
        Hunt Make(string title, string year, int? km, string loc, long price, long market,
            int below, int score, Liquidity liq, double hoursAgo, bool isNew, string? img,
            HuntState state = HuntState.Active) => new()
        {
            Id = id++, Title = title, ModelYear = year, MileageKm = km, Location = loc,
            CityId = 1, PriceToman = price, MarketMedianToman = market,
            BelowMarketPercent = below, Score = score, Liquidity = liq, State = state,
            PublishedAt = now.AddHours(-hoursAgo), IsNew = isNew, ImageFileName = img
        };

        // 1. آریسان
        var h1 = Make("آریسان دوگانه‌سوز شرکتی ۱۴۰۴ صفر", "۱۴۰۴", null, "پیروزی", 985_000_000, 1_200_000_000, 18, 87, Liquidity.High, 10.0 / 60, true, null);
        h1.Color = "سفید"; h1.BrandModel = "سایپا آریسان"; h1.GearboxType = "دنده‌ای"; h1.FuelType = "دوگانه‌سوز";
        h1.InsuranceMonths = 12; h1.EngineCondition = "سالم"; h1.ChassisCondition = "سالم و پلمپ";
        h1.BodyConditionText = "بدون رنگ"; h1.GearboxCondition = "سالم"; h1.BasePriceToman = 870_000_000;
        h1.SellerDescription = "صفر شرکتی، تحویل فوری. بیمه یک‌ساله کامل.";
        h1.SellerName = "شخصی (آقای احمدی)"; h1.SourceUrl = "https://divar.ir/v/example-1";
        h1.ImageCount = 4; h1.AiSummary = "آریسان صفر با قیمت ۱۸٪ زیر بازار، دوگانه‌سوز شرکتی.\nمناسب خرید مطمئن با سود سریع.";
        h1.ResellDaysMin = 2; h1.ResellDaysMax = 5;
        h1.Analysis = DefaultAnalysis(h1);
        Hunts.Add(h1);

        // 2. پژو ۲۰۷ — طبق سمپل
        var h2 = Make("پژو ۲۰۷ دنده‌ای پانوراما ۱۴۰۲", "۱۴۰۲", 28_000, "تهرانپارس", 1_340_000_000, 1_560_000_000, 14, 87, Liquidity.High, 35.0 / 60, true, "peugeot-207.webp");
        h2.Color = "سفید"; h2.BrandModel = "پژو ۲۰۷ پانوراما"; h2.GearboxType = "دنده‌ای"; h2.FuelType = "بنزینی";
        h2.InsuranceMonths = 7; h2.EngineCondition = "سالم"; h2.ChassisCondition = "سالم و پلمپ";
        h2.BodyConditionText = "یه لکه رنگ (گلگیر جلو)"; h2.GearboxCondition = "سالم"; h2.BasePriceToman = 890_000_000;
        h2.SellerDescription = "ماشین تمیز و کم‌کار، سرویس‌ها به‌موقع. فقط یه لکه رنگ گلگیر جلو. سند تک‌برگ، بیمه تا اردیبهشت. به‌خاطر خرید ماشین جدید، فوری فروشیه.";
        h2.SellerName = "شخصی (آقای مرادی)"; h2.SourceUrl = "https://divar.ir/v/example-2";
        h2.ImageCount = 6; h2.AiSummary = "۲۰۷ پانوراما با کارکرد کم، حدود ۱۴٪ زیر قیمت بازار و پرفروش.\nیه لکه رنگ گلگیر ذکر شده؛ قبل از خرید حتماً کارشناسی بدنه بگیر.";
        h2.ResellDaysMin = 3; h2.ResellDaysMax = 5;
        h2.Analysis = new HuntAnalysis
        {
            ComparableCount = 42, Confidence = "بالا",
            FairRangeMinToman = 1_500_000_000, FairRangeMaxToman = 1_620_000_000,
            Summary = "قیمت پایین + فروش سریع؛ یعنی حاشیه‌ی سود خوب با ریسک کم.",
            Factors = new[]
            {
                new AnalysisFactor("قیمت", "عالی", "۱۴٪ زیر میانه‌ی بازار", 93),
                new AnalysisFactor("نقدشوندگی", "عالی", "۲۰۷های مشابه معمولاً ۲ تا ۴ روزه فروش می‌رن", 92),
                new AnalysisFactor("کارکرد", "خیلی خوب", "۲۸ هزار کیلومتر؛ کمتر از میانگین این مدل", 86),
                new AnalysisFactor("وضعیت فنی", "خیلی خوب", "طبق آگهی: یه لکه رنگ گلگیر جلو", 80),
                new AnalysisFactor("کم‌ریسکی", "خوب", "۲ نکته قبل از خرید چک کن", 74)
            },
            Risks = new[] { "یه لکه رنگ ذکر شده", "بیمه‌ی بدنه نداره", "سند تک‌برگ ذکر شده" }
        };
        Hunts.Add(h2);

        // 3. MVM X55
        var h3 = Make("ام‌وی‌ام X55 پرو اکسلنت ۱۴۰۳", "۱۴۰۳", 11_000, "ستارخان", 2_980_000_000, 3_430_000_000, 13, 81, Liquidity.High, 2, false, "mvm-x55.webp");
        h3.Color = "مشکی"; h3.BrandModel = "ام‌وی‌ام X55 پرو"; h3.GearboxType = "اتوماتیک"; h3.FuelType = "بنزینی";
        h3.InsuranceMonths = 5; h3.EngineCondition = "سالم"; h3.ChassisCondition = "سالم و پلمپ";
        h3.BodyConditionText = "بدون رنگ"; h3.GearboxCondition = "سالم"; h3.BasePriceToman = 2_650_000_000;
        h3.SellerDescription = "فول‌آپشن، سرویس‌های نمایندگی. تایر نو.";
        h3.SellerName = "شخصی (آقای نوری)"; h3.SourceUrl = "https://divar.ir/v/example-3";
        h3.ImageCount = 5; h3.AiSummary = "X55 پرو با کارکرد کم و ۱۳٪ زیر بازار.\nشاسی‌بلند پرفروش با وضعیت عالی.";
        h3.ResellDaysMin = 4; h3.ResellDaysMax = 7;
        h3.Analysis = DefaultAnalysis(h3);
        Hunts.Add(h3);

        // 4. چانگان یونی‌وی
        var h4 = Make("چانگان یونی‌وی ۱۴۰۳", "۱۴۰۳", 6_000, "سعادت‌آباد", 4_600_000_000, 5_230_000_000, 12, 80, Liquidity.High, 1, false, "changan-univ.webp");
        h4.Color = "نقره‌ای"; h4.BrandModel = "چانگان یونی‌وی"; h4.GearboxType = "اتوماتیک"; h4.FuelType = "بنزینی";
        h4.InsuranceMonths = 10; h4.EngineCondition = "سالم"; h4.ChassisCondition = "سالم و پلمپ";
        h4.BodyConditionText = "بدون رنگ"; h4.GearboxCondition = "سالم"; h4.BasePriceToman = 4_100_000_000;
        h4.SellerDescription = "تک‌مالک، بدون تصادف. لاستیک‌ها ۹۰٪.";
        h4.SellerName = "شخصی (آقای کریمی)"; h4.SourceUrl = "https://divar.ir/v/example-4";
        h4.ImageCount = 7; h4.AiSummary = "یونی‌وی با کارکرد ناچیز و قیمت مناسب.\nشاسی‌بلند لوکس چینی با بازار فروش خوب.";
        h4.ResellDaysMin = 3; h4.ResellDaysMax = 6;
        h4.Analysis = DefaultAnalysis(h4);
        Hunts.Add(h4);

        // 5. تارا
        var h5 = Make("تارا اتوماتیک V4 ۱۴۰۳", "۱۴۰۳", 15_000, "پونک", 1_890_000_000, 2_150_000_000, 12, 79, Liquidity.High, 2, false, "tara.webp");
        h5.Color = "سفید"; h5.BrandModel = "ایران‌خودرو تارا"; h5.GearboxType = "اتوماتیک"; h5.FuelType = "بنزینی";
        h5.InsuranceMonths = 8; h5.EngineCondition = "سالم"; h5.ChassisCondition = "سالم و پلمپ";
        h5.BodyConditionText = "بدون رنگ"; h5.GearboxCondition = "سالم"; h5.BasePriceToman = 1_680_000_000;
        h5.SellerDescription = "سرویس نمایندگی، بدون خط‌وخش. تمیز و آماده تحویل.";
        h5.SellerName = "شخصی (خانم رحیمی)"; h5.SourceUrl = "https://divar.ir/v/example-5";
        h5.ImageCount = 5; h5.AiSummary = "تارا اتوماتیک با ۱۲٪ تخفیف نسبت به بازار.\nسدان ایرانی پرفروش با هزینه نگهداری پایین.";
        h5.ResellDaysMin = 3; h5.ResellDaysMax = 5;
        h5.Analysis = DefaultAnalysis(h5);
        Hunts.Add(h5);

        // 6. کوییک کراس
        var h6 = Make("کوییک کراس اتوماتیک ۱۴۰۲", "۱۴۰۲", 24_000, "تهرانسر", 945_000_000, 1_070_000_000, 12, 79, Liquidity.High, 3, false, "quick-cross.webp");
        h6.Color = "قرمز"; h6.BrandModel = "سایپا کوییک کراس"; h6.GearboxType = "اتوماتیک"; h6.FuelType = "بنزینی";
        h6.InsuranceMonths = 4; h6.EngineCondition = "سالم"; h6.ChassisCondition = "سالم و پلمپ";
        h6.BodyConditionText = "بدون رنگ"; h6.GearboxCondition = "سالم"; h6.BasePriceToman = 820_000_000;
        h6.SellerDescription = "کوییک کراس فول، رینگ اسپرت. بدون تصادف.";
        h6.SellerName = "شخصی (آقای حسینی)"; h6.SourceUrl = "https://divar.ir/v/example-6";
        h6.ImageCount = 4; h6.AiSummary = "کوییک کراس با ظاهر تمیز و ۱۲٪ زیر بازار.\nفروش سریع در رده‌ی زیر یک میلیارد.";
        h6.ResellDaysMin = 2; h6.ResellDaysMax = 4;
        h6.Analysis = DefaultAnalysis(h6);
        Hunts.Add(h6);

        // 7. نیسان N7
        var h7 = Make("نیسان N7 برقی ۲۰۲۵", "۲۰۲۵", 3_000, "شهرک غرب", 7_200_000_000, 7_910_000_000, 9, 78, Liquidity.Medium, 7, false, "nissan-n7.webp");
        h7.Color = "آبی"; h7.BrandModel = "نیسان N7"; h7.GearboxType = "اتوماتیک"; h7.FuelType = "برقی";
        h7.InsuranceMonths = 11; h7.EngineCondition = "سالم"; h7.ChassisCondition = "سالم و پلمپ";
        h7.BodyConditionText = "بدون رنگ"; h7.GearboxCondition = "سالم"; h7.BasePriceToman = 6_800_000_000;
        h7.SellerDescription = "برقی تمام، شارژر خانگی. باتری ۹۸٪ سلامت.";
        h7.SellerName = "شخصی (آقای صادقی)"; h7.SourceUrl = "https://divar.ir/v/example-7";
        h7.ImageCount = 6; h7.AiSummary = "N7 برقی با کارکرد ناچیز و ۹٪ زیر بازار.\nخودروی خاص با نقدشوندگی متوسط.";
        h7.ResellDaysMin = 7; h7.ResellDaysMax = 14;
        h7.Analysis = DefaultAnalysis(h7);
        Hunts.Add(h7);

        // 8. کیا سراتو
        var h8 = Make("کیا سراتو ۲۰۰۰ مدل ۲۰۱۷", "۲۰۱۷", 96_000, "یوسف‌آباد", 3_400_000_000, 3_820_000_000, 11, 77, Liquidity.High, 4, false, "kia-cerato.webp");
        h8.Color = "مشکی"; h8.BrandModel = "کیا سراتو ۲۰۰۰"; h8.GearboxType = "اتوماتیک"; h8.FuelType = "بنزینی";
        h8.InsuranceMonths = 3; h8.EngineCondition = "سالم"; h8.ChassisCondition = "سالم و پلمپ";
        h8.BodyConditionText = "دو لکه رنگ (درب عقب و گلگیر)"; h8.GearboxCondition = "سالم"; h8.BasePriceToman = 3_100_000_000;
        h8.SellerDescription = "سراتو فول‌آپشن، سانروف. سرویس‌ها کامل.";
        h8.SellerName = "شخصی (آقای عباسی)"; h8.SourceUrl = "https://divar.ir/v/example-8";
        h8.ImageCount = 5; h8.AiSummary = "سراتو کره‌ای با ۱۱٪ تخفیف.\nکارکرد بالا ولی بازار فروش خوب دارد.";
        h8.ResellDaysMin = 4; h8.ResellDaysMax = 7;
        h8.Analysis = DefaultAnalysis(h8);
        Hunts.Add(h8);

        // 9. دنا پلاس
        var h9 = Make("دنا پلاس توربو اتوماتیک ۱۴۰۲", "۱۴۰۲", 42_000, "نارمک", 2_450_000_000, 2_750_000_000, 11, 76, Liquidity.High, 3, false, "dena-plus.webp");
        h9.Color = "سفید"; h9.BrandModel = "ایران‌خودرو دنا پلاس توربو"; h9.GearboxType = "اتوماتیک"; h9.FuelType = "بنزینی";
        h9.InsuranceMonths = 6; h9.EngineCondition = "سالم"; h9.ChassisCondition = "سالم و پلمپ";
        h9.BodyConditionText = "بدون رنگ"; h9.GearboxCondition = "سالم"; h9.BasePriceToman = 2_200_000_000;
        h9.SellerDescription = "دنا پلاس توربو فول، صندلی برقی. بدون تصادف.";
        h9.SellerName = "شخصی (آقای محمدی)"; h9.SourceUrl = "https://divar.ir/v/example-9";
        h9.ImageCount = 5; h9.AiSummary = "دنا پلاس توربو، ۱۱٪ زیر بازار.\nسدان ایرانی لوکس با فروش سریع.";
        h9.ResellDaysMin = 3; h9.ResellDaysMax = 6;
        h9.Analysis = DefaultAnalysis(h9);
        Hunts.Add(h9);

        // 10. BMW 730Li
        var h10 = Make("بی‌ام‌و ۷۳۰Li ام‌پکیج ۲۰۱۷", "۲۰۱۷", 68_000, "زعفرانیه", 14_500_000_000, 16_290_000_000, 11, 75, Liquidity.Medium, 8, false, "bmw-730li.webp");
        h10.Color = "مشکی"; h10.BrandModel = "بی‌ام‌و ۷۳۰Li ام‌پکیج"; h10.GearboxType = "اتوماتیک"; h10.FuelType = "بنزینی";
        h10.InsuranceMonths = 2; h10.EngineCondition = "سالم"; h10.ChassisCondition = "سالم و پلمپ";
        h10.BodyConditionText = "یه لکه رنگ (درب راننده)"; h10.GearboxCondition = "سالم"; h10.BasePriceToman = 13_000_000_000;
        h10.SellerDescription = "۷۳۰ ام‌پکیج، سرویس‌های BMW. کارشناسی شده.";
        h10.SellerName = "شخصی (آقای رستمی)"; h10.SourceUrl = "https://divar.ir/v/example-10";
        h10.ImageCount = 8; h10.AiSummary = "بی‌ام‌و ۷۳۰ با ۱۱٪ تخفیف.\nخودروی لوکس با نقدشوندگی متوسط؛ مناسب خریدار صبور.";
        h10.ResellDaysMin = 10; h10.ResellDaysMax = 21;
        h10.Analysis = DefaultAnalysis(h10);
        Hunts.Add(h10);

        // 11. آریزو ۶ پرو
        var h11 = Make("فونیکس آریزو ۶ پرو ۱۴۰۲", "۱۴۰۲", 38_000, "جنت‌آباد", 2_150_000_000, 2_360_000_000, 9, 74, Liquidity.Medium, 4, false, "arrizo-6-pro.webp");
        h11.Color = "سفید"; h11.BrandModel = "فونیکس آریزو ۶ پرو"; h11.GearboxType = "اتوماتیک"; h11.FuelType = "بنزینی";
        h11.InsuranceMonths = 5; h11.EngineCondition = "سالم"; h11.ChassisCondition = "سالم و پلمپ";
        h11.BodyConditionText = "بدون رنگ"; h11.GearboxCondition = "سالم"; h11.BasePriceToman = 1_950_000_000;
        h11.SellerDescription = "آریزو ۶ پرو فول، دوربین ۳۶۰. تمیز.";
        h11.SellerName = "شخصی (آقای فرهادی)"; h11.SourceUrl = "https://divar.ir/v/example-11";
        h11.ImageCount = 5; h11.AiSummary = "آریزو ۶ پرو، ۹٪ زیر بازار.\nسدان چینی با امکانات خوب و نقدشوندگی متوسط.";
        h11.ResellDaysMin = 5; h11.ResellDaysMax = 10;
        h11.Analysis = DefaultAnalysis(h11);
        Hunts.Add(h11);

        // 12. هیوندای سوناتا
        var h12 = Make("هیوندای سوناتا LF مدل ۲۰۱۶", "۲۰۱۶", 110_000, "پاسداران", 3_850_000_000, 4_230_000_000, 9, 74, Liquidity.Medium, 5, false, "hyundai-sonata.webp");
        h12.Color = "نقره‌ای"; h12.BrandModel = "هیوندای سوناتا LF"; h12.GearboxType = "اتوماتیک"; h12.FuelType = "بنزینی";
        h12.InsuranceMonths = 4; h12.EngineCondition = "سالم"; h12.ChassisCondition = "سالم و پلمپ";
        h12.BodyConditionText = "سه لکه رنگ (درب‌ها)"; h12.GearboxCondition = "سالم"; h12.BasePriceToman = 3_500_000_000;
        h12.SellerDescription = "سوناتا LF فول، صندلی برقی و چرم. کارکرد بالا ولی سالم.";
        h12.SellerName = "شخصی (آقای جعفری)"; h12.SourceUrl = "https://divar.ir/v/example-12";
        h12.ImageCount = 6; h12.AiSummary = "سوناتا کره‌ای با کارکرد بالا و ۹٪ زیر بازار.\nریسک کارکرد وجود دارد ولی قیمت جذاب است.";
        h12.ResellDaysMin = 5; h12.ResellDaysMax = 10;
        h12.Analysis = DefaultAnalysis(h12);
        Hunts.Add(h12);

        // 13. تویوتا پرادو
        var h13 = Make("تویوتا لندکروزر پرادو ۲۰۲۵", "۲۰۲۵", null, "فرمانیه", 26_500_000_000, 29_120_000_000, 9, 73, Liquidity.Medium, 7, false, "toyota-prado.webp");
        h13.Color = "سفید صدفی"; h13.BrandModel = "تویوتا لندکروزر پرادو"; h13.GearboxType = "اتوماتیک"; h13.FuelType = "بنزینی";
        h13.InsuranceMonths = 12; h13.EngineCondition = "سالم"; h13.ChassisCondition = "سالم و پلمپ";
        h13.BodyConditionText = "بدون رنگ"; h13.GearboxCondition = "سالم"; h13.BasePriceToman = 25_000_000_000;
        h13.SellerDescription = "پرادو صفر ۲۰۲۵، تحویل فوری. گارانتی نمایندگی.";
        h13.SellerName = "شخصی (آقای هاشمی)"; h13.SourceUrl = "https://divar.ir/v/example-13";
        h13.ImageCount = 8; h13.AiSummary = "پرادو صفر با ۹٪ تخفیف نسبت به بازار.\nشاسی‌بلند لوکس ژاپنی؛ نقدشوندگی در این رده متوسط است.";
        h13.ResellDaysMin = 14; h13.ResellDaysMax = 30;
        h13.Analysis = DefaultAnalysis(h13);
        Hunts.Add(h13);

        // 14. تویوتا کمری (حذف‌شده)
        var h14 = Make("تویوتا کمری هیبرید ۲۰۱۸", "۲۰۱۸", 76_000, "ونک", 6_900_000_000, 7_500_000_000, 8, 71, Liquidity.Medium, 5, false, null, HuntState.Removed);
        h14.Color = "خاکستری"; h14.BrandModel = "تویوتا کمری هیبرید"; h14.GearboxType = "اتوماتیک"; h14.FuelType = "هیبرید";
        h14.InsuranceMonths = 3; h14.EngineCondition = "سالم"; h14.ChassisCondition = "سالم و پلمپ";
        h14.BodyConditionText = "بدون رنگ"; h14.GearboxCondition = "سالم"; h14.BasePriceToman = 6_200_000_000;
        h14.SellerDescription = "کمری هیبرید فول، مصرف پایین.";
        h14.SellerName = "شخصی"; h14.SourceUrl = "https://divar.ir/v/example-14";
        h14.ImageCount = 5; h14.AiSummary = "کمری هیبرید ۲۰۱۸ با ۸٪ زیر بازار.\nآگهی حذف شده است.";
        h14.ResellDaysMin = 7; h14.ResellDaysMax = 14;
        h14.Analysis = DefaultAnalysis(h14);
        Hunts.Add(h14);

        // 15. بنز E250
        var h15 = Make("بنز E250 مدل ۲۰۱۷", "۲۰۱۷", 85_000, "الهیه", 19_800_000_000, 21_760_000_000, 9, 70, Liquidity.Medium, 10, false, null);
        h15.Color = "مشکی"; h15.BrandModel = "مرسدس بنز E250"; h15.GearboxType = "اتوماتیک"; h15.FuelType = "بنزینی";
        h15.InsuranceMonths = 1; h15.EngineCondition = "سالم"; h15.ChassisCondition = "سالم و پلمپ";
        h15.BodyConditionText = "دو لکه رنگ (سپر و گلگیر)"; h15.GearboxCondition = "سالم"; h15.BasePriceToman = 18_000_000_000;
        h15.SellerDescription = "E250 فول، AMG پکیج. کارشناسی شده.";
        h15.SellerName = "شخصی (آقای شریفی)"; h15.SourceUrl = "https://divar.ir/v/example-15";
        h15.ImageCount = 7; h15.AiSummary = "بنز E250 با ۹٪ تخفیف.\nخودروی لوکس آلمانی؛ نقدشوندگی متوسط و هزینه نگهداری بالا.";
        h15.ResellDaysMin = 14; h15.ResellDaysMax = 28;
        h15.Analysis = DefaultAnalysis(h15);
        Hunts.Add(h15);
    }

    private static HuntAnalysis DefaultAnalysis(Hunt h)
    {
        int pScore = Math.Min(99, h.BelowMarketPercent * 6 + 10);
        int lScore = h.Liquidity == Liquidity.High ? 85 + Random.Shared.Next(0, 10) : 55 + Random.Shared.Next(0, 15);
        int kmScore = h.MileageKm switch
        {
            null or 0 => 95,
            < 30_000 => 86,
            < 60_000 => 72,
            < 100_000 => 60,
            _ => 48
        };
        int techScore = 75 + Random.Shared.Next(0, 15);
        int riskScore = 65 + Random.Shared.Next(0, 20);

        string rate(int s) => s >= 90 ? "عالی" : s >= 75 ? "خیلی خوب" : s >= 60 ? "خوب" : "متوسط";
        string kmText = h.MileageKm switch
        {
            null or 0 => "صفر کیلومتر؛ خودروی نو",
            < 30_000 => $"{h.MileageKm.Value / 1000} هزار کیلومتر؛ کمتر از میانگین",
            < 60_000 => $"{h.MileageKm.Value / 1000} هزار کیلومتر؛ نزدیک میانگین",
            _ => $"{h.MileageKm.Value / 1000} هزار کیلومتر؛ بیشتر از میانگین"
        };
        string liqText = h.Liquidity == Liquidity.High ? "فروش سریع؛ معمولاً ۳ تا ۵ روزه" : "فروش متوسط؛ ممکنه ۱ تا ۳ هفته طول بکشه";

        long fair = h.MarketMedianToman;
        var risks = new List<string>();
        if (h.BodyConditionText is not null && h.BodyConditionText != "بدون رنگ")
            risks.Add("رنگ‌شدگی ذکر شده");
        if (h.InsuranceMonths is < 3)
            risks.Add("بیمه‌ی شخص ثالث رو به اتمام");
        if (h.MileageKm is > 80_000)
            risks.Add("کارکرد بالا");
        if (risks.Count == 0)
            risks.Add("ریسک خاصی شناسایی نشد");

        return new HuntAnalysis
        {
            ComparableCount = 15 + Random.Shared.Next(5, 40),
            Confidence = h.BelowMarketPercent >= 12 ? "بالا" : h.BelowMarketPercent >= 8 ? "متوسط" : "پایین",
            FairRangeMinToman = fair - fair / 20,
            FairRangeMaxToman = fair + fair / 20,
            Summary = $"قیمت {h.BelowMarketPercent}٪ زیر بازار؛ {(h.Liquidity == Liquidity.High ? "فروش سریع و سود مناسب" : "نقدشوندگی متوسط؛ نیاز به صبر بیشتر")}.",
            Factors = new[]
            {
                new AnalysisFactor("قیمت", rate(pScore), $"{h.BelowMarketPercent}٪ زیر میانه‌ی بازار", pScore),
                new AnalysisFactor("نقدشوندگی", rate(lScore), liqText, lScore),
                new AnalysisFactor("کارکرد", rate(kmScore), kmText, kmScore),
                new AnalysisFactor("وضعیت فنی", rate(techScore), h.BodyConditionText ?? "اطلاعات ناکافی", techScore),
                new AnalysisFactor("کم‌ریسکی", rate(riskScore), $"{risks.Count} نکته قبل از خرید", riskScore)
            },
            Risks = risks.ToArray()
        };
    }

    private void SeedBells(DateTimeOffset now)
    {
        // حساب نمونه: دو زنگوله‌ی روشن (۱–۱٫۵ب و ۱٫۵–۲ب)، بقیه خاموش.
        BellAlerts.Add(new BellAlert
        {
            Id = 1,
            UserId = DemoUserId,
            Category = PriceCategory.From1To1_5B,
            ActivatedAt = now.AddHours(-3),
            ExpiresAt = now.AddHours(21)
        });
        BellAlerts.Add(new BellAlert
        {
            Id = 2,
            UserId = DemoUserId,
            Category = PriceCategory.From1_5To2B,
            ActivatedAt = now.AddHours(-16),
            ExpiresAt = now.AddHours(8)
        });
    }

    private void SeedAppraisals(DateTimeOffset now)
    {
        Appraisals.Add(new Appraisal { Id = 1, UserId = DemoUserId, AdUrl = "https://example.com/ad/207", Title = "پژو ۲۰۷ پانوراما ۱۴۰۲", Score = 87, Verdict = "فرصت طلایی", CreatedAt = now.AddDays(-5) });
        Appraisals.Add(new Appraisal { Id = 2, UserId = DemoUserId, AdUrl = "https://example.com/ad/tara", Title = "تارا اتوماتیک ۱۴۰۳", Score = 64, Verdict = "منصفانه", CreatedAt = now.AddDays(-8) });
        Appraisals.Add(new Appraisal { Id = 3, UserId = DemoUserId, AdUrl = "https://example.com/ad/dena", Title = "دنا پلاس ۱۴۰۱", Score = 38, Verdict = "گران", CreatedAt = now.AddDays(-14) });
    }
}
