using Shekar.Domain.Enums;

namespace Shekar.Domain.Entities;

/// <summary>
/// آگهی خام خودرو که کراولر از منبع (دیوار/باما) دریافت و ذخیره می‌کند.
/// این لایه‌ی خام، از لایه‌ی «تاپ‌ها» (Hunts) جداست تا پایپ‌لاین قابل‌بازپخش بماند.
/// </summary>
public class CarListing
{
    public long Id { get; set; }

    /// <summary>منبع آگهی: 1=دیوار، 2=باما.</summary>
    public ListingSource Source { get; set; }

    /// <summary>شناسه‌ی آگهی در منبع؛ همراه Source برای جلوگیری از تکرار یکتا می‌شود.</summary>
    public string ExternalId { get; set; } = string.Empty;

    /// <summary>آدرس آگهی در منبع.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>عنوان آگهی.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>توضیحات آگهی.</summary>
    public string? Description { get; set; }

    /// <summary>مدل خودرو پس از نرمال‌سازی؛ NULL تا وقتی تطبیق انجام شود.</summary>
    public int? VehicleModelId { get; set; }

    /// <summary>برند خام استخراج‌شده از آگهی.</summary>
    public string? RawBrand { get; set; }

    /// <summary>مدل خام استخراج‌شده از آگهی.</summary>
    public string? RawModel { get; set; }

    /// <summary>تریم خام استخراج‌شده از آگهی.</summary>
    public string? RawTrim { get; set; }

    /// <summary>
    /// شناسه‌ی قطعیِ مدل در منبع (brand_model دیوار)؛ مبنای نگاشت/رجیستر خودکارِ مدل.
    /// </summary>
    public string? SourceModelKey { get; set; }

    /// <summary>سال مدل.</summary>
    public int? ModelYear { get; set; }

    /// <summary>کارکرد (کیلومتر)؛ NULL=صفر/نامشخص.</summary>
    public int? MileageKm { get; set; }

    /// <summary>رنگ خودرو.</summary>
    public string? Color { get; set; }

    /// <summary>وضعیت بدنه.</summary>
    public BodyCondition BodyCondition { get; set; } = BodyCondition.Unknown;

    /// <summary>قیمت آگهی (تومان)؛ آگهی‌های بدون قیمت معتبر ذخیره نمی‌شوند.</summary>
    public long PriceToman { get; set; }

    /// <summary>شهر آگهی (نگاشت‌شده به Cities)؛ NULL اگر نامشخص.</summary>
    public int? CityId { get; set; }

    /// <summary>نام شهر/محله‌ی خام آگهی.</summary>
    public string? RawLocation { get; set; }

    /// <summary>شماره‌ی آگهی‌دهنده (برای فیلتر شماره‌های بلاک‌شده).</summary>
    public string? AdvertiserPhone { get; set; }

    /// <summary>تعداد عکس آگهی؛ آگهیِ بدون عکس ذخیره نمی‌شود (ImageCount &gt; 0).</summary>
    public int ImageCount { get; set; }

    /// <summary>آدرس اولین عکس.</summary>
    public string? FirstImageUrl { get; set; }

    /// <summary>زمان انتشار آگهی در منبع (فیلتر ۲۴ ساعت اخیر روی این فیلد).</summary>
    public DateTimeOffset PublishedAt { get; set; }

    /// <summary>زمان اولین دریافت توسط کراولر.</summary>
    public DateTimeOffset FetchedAt { get; set; }

    /// <summary>آخرین باری که در کراول همچنان دیده شد (برای تشخیص حذف).</summary>
    public DateTimeOffset LastSeenAt { get; set; }

    /// <summary>وضعیت: 1=فعال، 2=حذف‌شده (دیگر در منبع نیست).</summary>
    public ListingState State { get; set; } = ListingState.Active;

    /// <summary>زمان تشخیص حذف.</summary>
    public DateTimeOffset? RemovedAt { get; set; }

    /// <summary>آیا مرحله‌ی اسکورینگ این آگهی را پردازش کرده.</summary>
    public bool IsScored { get; set; }

    /// <summary>اثرانگشت محتوا (phone+title+price) برای تشخیص آگهی تکراریِ نزدیک.</summary>
    public string? ContentHash { get; set; }

    /// <summary>بدنه‌ی خام آگهی (JSON) برای پردازش مجدد و دیباگ.</summary>
    public string? RawJson { get; set; }
}
