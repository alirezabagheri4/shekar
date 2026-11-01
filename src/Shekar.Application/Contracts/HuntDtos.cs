using Shekar.Domain.Enums;

namespace Shekar.Application.Contracts;

/// <summary>یک کارت شکار در لیست.</summary>
public sealed record HuntCardDto
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string ModelYear { get; init; } = string.Empty;
    public int? MileageKm { get; init; }
    public string Location { get; init; } = string.Empty;
    public long PriceToman { get; init; }
    public long MarketMedianToman { get; init; }
    public int BelowMarketPercent { get; init; }
    public int Score { get; init; }

    /// <summary>R8 — امتیاز ≥ ۸۵.</summary>
    public bool IsGolden { get; init; }

    public bool IsNew { get; init; }
    public Liquidity Liquidity { get; init; }

    /// <summary>R9 — سود خالص تخمینی (تومان).</summary>
    public long EstimatedProfitToman { get; init; }

    public DateTimeOffset PublishedAt { get; init; }
    public string? ImageFileName { get; init; }
    public PriceCategory Category { get; init; }

    /// <summary>R7 — آگهی حذف شده؛ بدون لینک/جزئیات.</summary>
    public bool IsRemoved { get; init; }
}

/// <summary>بنر آمارِ حالت قفل (R4) برای کاربر بدون اشتراک.</summary>
public sealed record LockedBannerDto
{
    public int TodayHuntCount { get; init; }
    public int AverageBelowMarketPercent { get; init; }
    public long MaxProfitToman { get; init; }
    public int GoldenCount { get; init; }
}

/// <summary>خروجی لیست شکارها.</summary>
public sealed record HuntListDto
{
    public PriceCategory? Category { get; init; }
    public HuntDay Day { get; init; }
    public HuntSortOrder Sort { get; init; }

    /// <summary>R4 — آیا لیست برای کاربر جاری قفل است.</summary>
    public bool IsLocked { get; init; }

    /// <summary>R11 — تعداد «شکار فعال» (حذف‌نشده‌ها).</summary>
    public int ActiveCount { get; init; }

    public IReadOnlyList<HuntCardDto> Cards { get; init; } = Array.Empty<HuntCardDto>();

    /// <summary>فقط وقتی <see cref="IsLocked"/> درست است پر می‌شود.</summary>
    public LockedBannerDto? LockedBanner { get; init; }
}

/// <summary>سنجه‌ی امتیاز در جزئیات.</summary>
public sealed record AnalysisFactorDto(string Name, string Rating, string Detail, int Score = 0);

/// <summary>جزئیات کامل یک شکار (برای صفحه‌ی hunt-detail و نتیجه‌ی کارشناسی).</summary>
public sealed record HuntDetailDto
{
    public HuntCardDto Card { get; init; } = default!;
    public int ComparableCount { get; init; }
    public string Confidence { get; init; } = string.Empty;
    public long FairRangeMinToman { get; init; }
    public long FairRangeMaxToman { get; init; }
    public string Summary { get; init; } = string.Empty;
    public IReadOnlyList<AnalysisFactorDto> Factors { get; init; } = Array.Empty<AnalysisFactorDto>();
    public IReadOnlyList<string> Risks { get; init; } = Array.Empty<string>();

    // ── مشخصات فنی خودرو ──
    public string? Color { get; init; }
    public string? BrandModel { get; init; }
    public string? GearboxType { get; init; }
    public string? FuelType { get; init; }
    public int? InsuranceMonths { get; init; }
    public string? EngineCondition { get; init; }
    public string? ChassisCondition { get; init; }
    public string? BodyConditionText { get; init; }
    public string? GearboxCondition { get; init; }
    public long? BasePriceToman { get; init; }
    public string? SellerDescription { get; init; }
    public string? SellerName { get; init; }
    public string? SourceUrl { get; init; }
    public int ImageCount { get; init; }
    public string? AiSummary { get; init; }
    public int? ResellDaysMin { get; init; }
    public int? ResellDaysMax { get; init; }

    /// <summary>R9 — اجزای محاسبه‌ی سود.</summary>
    public long TransactionCostToman { get; init; }
}

/// <summary>خلاصه‌ی یک دسته‌ی قیمتی برای سایدبار/صفحه‌ی دسته‌ها.</summary>
public sealed record CategorySummaryDto
{
    public PriceCategory Category { get; init; }
    public string Label { get; init; } = string.Empty;
    public int TodayCount { get; init; }
    public string? ImageFileName { get; init; }

    /// <summary>R6 — آیا زنگوله‌ی این دسته برای کاربر جاری روشن است.</summary>
    public bool BellOn { get; init; }
}
