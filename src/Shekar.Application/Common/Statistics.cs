namespace Shekar.Application.Common;

/// <summary>محاسبات آماری برای اگریگیشن قیمت (میانه و چارک‌ها).</summary>
public static class Statistics
{
    /// <summary>میانه‌ی یک مجموعه‌ی مرتب‌نشده.</summary>
    public static long Median(IReadOnlyList<long> values) => Percentile(values, 50);

    /// <summary>صدکِ درخواستی (۰ تا ۱۰۰) با درون‌یابی خطی.</summary>
    public static long Percentile(IReadOnlyList<long> values, double percentile)
    {
        if (values.Count == 0)
        {
            return 0;
        }

        long[] sorted = values.OrderBy(v => v).ToArray();
        if (sorted.Length == 1)
        {
            return sorted[0];
        }

        double rank = percentile / 100.0 * (sorted.Length - 1);
        int low = (int)Math.Floor(rank);
        int high = (int)Math.Ceiling(rank);
        double frac = rank - low;
        return (long)Math.Round(sorted[low] + (sorted[high] - sorted[low]) * frac);
    }

    public static long Average(IReadOnlyList<long> values) =>
        values.Count == 0 ? 0 : (long)Math.Round(values.Average());

    /// <summary>
    /// پرت‌های قیمت را با حصارِ IQR (۱.۵×) حذف می‌کند تا میانگین/چارک در برابر آگهی‌های
    /// غلط (قیمتِ تایپی، اقساطی، اسپم) مقاوم شوند. برای نمونه‌های کوچک (کمتر از
    /// <paramref name="minCountToTrim"/>) و زمانی که تریم بیش از نیمی را حذف کند، داده‌ی خام برمی‌گردد.
    /// </summary>
    public static IReadOnlyList<long> TrimOutliers(IReadOnlyList<long> values, int minCountToTrim = 8)
    {
        if (values.Count < minCountToTrim)
        {
            return values;
        }

        long q1 = Percentile(values, 25);
        long q3 = Percentile(values, 75);
        long iqr = q3 - q1;
        if (iqr <= 0)
        {
            return values; // پراکندگی صفر؛ چیزی برای تریم نیست
        }

        long fence = (long)(1.5 * iqr);
        long lo = q1 - fence, hi = q3 + fence;
        var kept = values.Where(v => v >= lo && v <= hi).ToList();

        // ایمنی: اگر تریم بیش از نیمی را حذف کرد، احتمالاً پراکندگیِ واقعی است؛ خام را نگه دار.
        return kept.Count >= Math.Max(3, values.Count / 2) ? kept : values;
    }
}
