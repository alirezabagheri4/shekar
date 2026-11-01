using Shekar.Domain.Enums;

namespace Shekar.Domain.Entities;

/// <summary>
/// یک «شکار»: آگهی خودروی زیر قیمت بازار با نقدشوندگی بالا.
/// فیلدها داده‌ی خام هستند؛ محاسبات قوانین (سود، فرصت طلایی، …) در لایه‌ی Application انجام می‌شود.
/// </summary>
public class Hunt
{
    public int Id { get; set; }

    /// <summary>عنوان آگهی، مثل «پژو ۲۰۷ دنده‌ای پانوراما ۱۴۰۲».</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>سال مدل (شمسی یا میلادی، همان‌طور که در آگهی آمده).</summary>
    public string ModelYear { get; set; } = string.Empty;

    /// <summary>کارکرد به کیلومتر؛ برای خودروی صفر <c>null</c>.</summary>
    public int? MileageKm { get; set; }

    /// <summary>محله/منطقه‌ی آگهی، مثل «تهرانپارس».</summary>
    public string Location { get; set; } = string.Empty;

    public int CityId { get; set; }

    /// <summary>قیمت آگهی به تومان.</summary>
    public long PriceToman { get; set; }

    /// <summary>میانه‌ی قیمت بازار به تومان.</summary>
    public long MarketMedianToman { get; set; }

    /// <summary>درصد زیر بازار (نمایش داده‌شده در نمونه).</summary>
    public int BelowMarketPercent { get; set; }

    /// <summary>امتیاز شکار، ۰ تا ۱۰۰.</summary>
    public int Score { get; set; }

    public Liquidity Liquidity { get; set; }

    public HuntState State { get; set; } = HuntState.Active;

    public DateTimeOffset PublishedAt { get; set; }

    /// <summary>آیا برچسب «تازه» را نمایش دهد.</summary>
    public bool IsNew { get; set; }

    /// <summary>نام فایل تصویر در wwwroot/assets/ads؛ <c>null</c> یعنی تصویر placeholder.</summary>
    public string? ImageFileName { get; set; }

    /// <summary>شماره‌ی آگهی‌دهنده؛ برای حذف آگهی‌های شماره‌های بلاک‌شده از پیشنهادها.</summary>
    public string? AdvertiserPhone { get; set; }

    /// <summary>آگهی خامِ منبعِ این شکار (خروجی اسکورینگ)؛ NULL برای شکارهای seed/دستی.</summary>
    public long? SourceListingId { get; set; }

    // ── مشخصات فنی خودرو (از صفحه‌ی جزئیات آگهی منبع) ──

    public string? Color { get; set; }
    public string? BrandModel { get; set; }
    public string? GearboxType { get; set; }
    public string? FuelType { get; set; }
    public int? InsuranceMonths { get; set; }
    public string? EngineCondition { get; set; }
    public string? ChassisCondition { get; set; }
    public string? BodyConditionText { get; set; }
    public string? GearboxCondition { get; set; }
    public long? BasePriceToman { get; set; }
    public string? SellerDescription { get; set; }
    public string? SellerName { get; set; }
    public string? SourceUrl { get; set; }
    public int ImageCount { get; set; }
    public string? AiSummary { get; set; }
    public int? ResellDaysMin { get; set; }
    public int? ResellDaysMax { get; set; }

    /// <summary>تحلیل تفصیلی برای صفحه‌ی جزئیات؛ ممکن است هنوز موجود نباشد.</summary>
    public HuntAnalysis? Analysis { get; set; }
}
