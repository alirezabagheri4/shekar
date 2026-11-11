using Shekar.Domain.Enums;

namespace Shekar.Domain.Entities;

/// <summary>
/// آمار یک «سگمنت» بازار که Job ساعتی محاسبه می‌کند. مرجع تشخیص زیرقیمت‌بودن در اسکورینگ.
/// کلید سگمنت: مدل + سال + باکتِ‌کارکرد + وضعیت‌بدنه [+ رنگ اگر خودرو گران باشد].
/// </summary>
public class MarketSegmentStat
{
    public int Id { get; set; }

    /// <summary>کلید متنیِ یکتای سگمنت (برای upsert سریع).</summary>
    public string SegmentKey { get; set; } = string.Empty;

    /// <summary>
    /// سطحِ ویژگیِ سگمنت در سلسله‌مراتب: ۰=دقیق (مدل+سال+کارکرد+بدنه+رنگ)،
    /// ۱=بدونِ رنگ، ۲=بدونِ بدنه (مدل+سال+کارکرد). اسکورینگ دقیق‌ترین سطحِ قابل‌اتکا را می‌گیرد.
    /// </summary>
    public byte Level { get; set; }

    public int VehicleModelId { get; set; }

    public int ModelYear { get; set; }

    public MileageBucket MileageBucket { get; set; }

    public BodyCondition BodyCondition { get; set; }

    /// <summary>کلید رنگ؛ NULL یعنی رنگ در این سگمنت دخیل نیست (خودروی غیرگران).</summary>
    public string? ColorKey { get; set; }

    /// <summary>تعداد نمونه‌ی به‌کاررفته (پس از حذفِ پرت‌ها).</summary>
    public int SampleCount { get; set; }

    /// <summary>تعداد نمونه‌ی خام (پیش از حذفِ پرت)؛ برای شفافیتِ کیفیتِ داده.</summary>
    public int RawSampleCount { get; set; }

    /// <summary>میانه‌ی قیمت (تومان) — مرجع اصلی (مقاوم در برابر آگهی‌های پرت).</summary>
    public long MedianPriceToman { get; set; }

    /// <summary>میانگین قیمت (تومان).</summary>
    public long AveragePriceToman { get; set; }

    /// <summary>کمینه‌ی قیمتِ نمونه‌ی تریم‌شده (کفِ معقولِ بازار).</summary>
    public long MinPriceToman { get; set; }

    /// <summary>چارک اول (۲۵٪) قیمت.</summary>
    public long P25PriceToman { get; set; }

    /// <summary>چارک سوم (۷۵٪) قیمت.</summary>
    public long P75PriceToman { get; set; }

    /// <summary>پراکندگیِ نسبی = IQR÷میانه به درصد؛ هرچه کمتر، مرجعِ قیمت مطمئن‌تر.</summary>
    public int SpreadPercent { get; set; }

    /// <summary>کارکردِ مرجع (کیلومتر) = میانه‌ی کارکردِ نمونه‌های این سگمنت؛ مبنای تعدیلِ کارکرد.</summary>
    public int RefMileageKm { get; set; }

    /// <summary>
    /// شیبِ قیمت به‌ازای هر ۱۰۰۰ کیلومتر کارکرد (تومان)، برآوردشده در سطحِ (مدل+سال)؛ معمولاً منفی
    /// (کارکردِ بیشتر → قیمتِ کمتر). ۰ یعنی داده برای تعدیل کافی نبود. در اسکورینگ «قیمتِ موردانتظارِ
    /// همین کارکرد» از روی میانه + شیب × اختلافِ کارکرد ساخته می‌شود.
    /// </summary>
    public long MileageSlopeTomanPerK { get; set; }

    /// <summary>
    /// آیا این سگمنت برای تشخیصِ زیرقیمت «قابل‌اتکا» است (نمونه‌ی کافی + پراکندگیِ محدود + تازه).
    /// سگمنتِ غیرقابل‌اتکا نباید مرجعِ قطعیِ اسکورینگ باشد.
    /// </summary>
    public bool IsReliable { get; set; }

    /// <summary>طول پنجره‌ی زمانی نمونه‌گیری (روز).</summary>
    public int WindowDays { get; set; }

    /// <summary>زمان آخرین محاسبه.</summary>
    public DateTimeOffset ComputedAt { get; set; }
}
