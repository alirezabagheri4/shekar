namespace Shekar.Domain.Entities;

/// <summary>تحلیل تفصیلی یک شکار برای صفحه‌ی جزئیات و نتیجه‌ی کارشناسی.</summary>
public class HuntAnalysis
{
    /// <summary>تعداد آگهی‌های مشابه که تحلیل بر اساس آن‌هاست.</summary>
    public int ComparableCount { get; set; }

    /// <summary>سطح اطمینان نمایشی، مثل «بالا».</summary>
    public string Confidence { get; set; } = string.Empty;

    /// <summary>کف بازه‌ی منصفانه (تومان).</summary>
    public long FairRangeMinToman { get; set; }

    /// <summary>سقف بازه‌ی منصفانه (تومان).</summary>
    public long FairRangeMaxToman { get; set; }

    /// <summary>خلاصه‌ی متنی هوش مصنوعی.</summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>سنجه‌های امتیاز (قیمت، نقدشوندگی، کارکرد، وضعیت فنی، ریسک).</summary>
    public IReadOnlyList<AnalysisFactor> Factors { get; set; } = Array.Empty<AnalysisFactor>();

    /// <summary>ریسک‌ها و نکته‌های پیش از خرید.</summary>
    public IReadOnlyList<string> Risks { get; set; } = Array.Empty<string>();
}

/// <summary>یک سنجه‌ی امتیاز با برچسب کیفی و توضیح.</summary>
public sealed record AnalysisFactor(string Name, string Rating, string Detail, int Score = 0);
