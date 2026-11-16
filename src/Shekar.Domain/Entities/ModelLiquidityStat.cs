namespace Shekar.Domain.Entities;

/// <summary>
/// آمارِ غلتانِ «نقدشوندگی» در سطحِ هر مدلِ خودرو، که در پاسِ اگریگیشن از آگهی‌های خام ساخته می‌شود.
/// سرعتِ واقعیِ ترک‌کردنِ بازار (روزهای‌روی‌بازار و نرخِ حذف) را می‌سنجد تا در گامِ بعد جایگزینِ
/// «نقدشوندگیِ ثابتِ کاتالوگ» در اسکورینگ شود. این لایه فقط داده می‌سازد؛ مصرفِ آن در فازِ بعد.
/// </summary>
public class ModelLiquidityStat
{
    public int Id { get; set; }

    /// <summary>مدلِ خودرو (کلیدِ یکتا برای upsert).</summary>
    public int VehicleModelId { get; set; }

    /// <summary>طول پنجره‌ی زمانی نمونه‌گیری (روز).</summary>
    public int WindowDays { get; set; }

    /// <summary>تعداد کلِ آگهی‌های این مدل که در پنجره «دیده» شده‌اند (فعال + حذف‌شده) — کوهورتِ مبنا.</summary>
    public int SampleCount { get; set; }

    /// <summary>تعداد آگهی‌های حذف‌شده‌ی این مدل در پنجره (پراکسیِ «فروخته‌شده»؛ مبنای DOM و نرخِ حذف).</summary>
    public int RemovedCount { get; set; }

    /// <summary>تعداد آگهی‌های هنوز فعال (هنوز نفروخته؛ برای لحاظِ سانسور/کهنگیِ شواهد در گامِ مصرف).</summary>
    public int ActiveCount { get; set; }

    /// <summary>میانه‌ی روزهای‌روی‌بازار (DOM) روی آگهی‌های حذف‌شده؛ کمتر = گردشِ سریع‌تر = نقدشونده‌تر.</summary>
    public int MedianDomDays { get; set; }

    /// <summary>چارک اولِ روزهای‌روی‌بازار (سریع‌ترین ربع).</summary>
    public int P25DomDays { get; set; }

    /// <summary>نرخِ حذف در پنجره (٪) = حذف‌شده ÷ کلِ کوهورت؛ بالاتر = گردشِ بیشتر.</summary>
    public int RemovalRatePercent { get; set; }

    /// <summary>
    /// آیا نقدشوندگیِ داده‌محورِ این مدل «قابل‌اتکا»ست (حذفِ کافی). مدلِ کم‌حذف باید در گامِ مصرف
    /// به‌سمتِ نقدشوندگیِ ثابتِ کاتالوگ shrink شود (پرهیز از پرتگاهِ cold-start و اریبِ سانسور).
    /// </summary>
    public bool IsReliable { get; set; }

    /// <summary>زمان آخرین محاسبه.</summary>
    public DateTimeOffset ComputedAt { get; set; }
}
