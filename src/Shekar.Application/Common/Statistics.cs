namespace Shekar.Application.Common;

/// <summary>خلاصه‌ی آماریِ یک سگمنت (پس از حذفِ پرت‌ها).</summary>
public readonly record struct PriceStats(
    long Min, long P25, long Median, long Average, long P75, long Max, int SampleCount)
{
    /// <summary>پراکندگیِ نسبی = IQR÷میانه به درصد؛ معیارِ اعتمادِ سگمنت (تنگ‌تر → مطمئن‌تر).</summary>
    public int SpreadPercent => Median > 0 ? (int)Math.Round((P75 - P25) * 100.0 / Median) : 0;
}

/// <summary>محاسبات آماری برای اگریگیشن قیمت (میانه، میانگین، چارک‌ها) با حذفِ مقاومِ پرت.</summary>
public static class Statistics
{
    /// <summary>میانه‌ی یک مجموعه‌ی مرتب‌نشده.</summary>
    public static long Median(IReadOnlyList<long> values) => Percentile(values, 50);

    /// <summary>صدکِ درخواستی (۰ تا ۱۰۰) با درون‌یابی خطی.</summary>
    public static long Percentile(IReadOnlyList<long> values, double percentile) =>
        values.Count == 0 ? 0 : PercentileSorted(SortedCopy(values), percentile);

    public static long Average(IReadOnlyList<long> values) =>
        values.Count == 0 ? 0 : (long)Math.Round(values.Average());

    /// <summary>
    /// پرت‌های قیمت را با حصارِ IQR (۱.۵×) حذف می‌کند تا میانگین/چارک در برابر آگهی‌های
    /// غلط (قیمتِ تایپی، اقساطی، اسپم) مقاوم شوند. برای نمونه‌های کوچک (کمتر از
    /// <paramref name="minCountToTrim"/>) و زمانی که تریم بیش از نیمی را حذف کند، داده‌ی خام برمی‌گردد.
    /// </summary>
    public static IReadOnlyList<long> TrimOutliers(IReadOnlyList<long> values, int minCountToTrim = 8) =>
        TrimSorted(SortedCopy(values), minCountToTrim);

    /// <summary>
    /// پرت‌ها را حذف و همه‌ی آماره‌ها را با <b>یک‌بار</b> مرتب‌سازی حساب می‌کند
    /// (به‌جای مرتب‌سازیِ تکراری در هر فراخوانیِ میانه/میانگین/چارک). مسیرِ داغِ اگریگیشن.
    /// </summary>
    public static PriceStats Summarize(IReadOnlyList<long> rawPrices, int minCountToTrim = 8)
    {
        if (rawPrices.Count == 0)
        {
            return new PriceStats(0, 0, 0, 0, 0, 0, 0);
        }

        long[] kept = TrimSorted(SortedCopy(rawPrices), minCountToTrim);
        return new PriceStats(
            Min: kept[0],                       // مرتب‌شده
            P25: PercentileSorted(kept, 25),
            Median: PercentileSorted(kept, 50),
            Average: (long)Math.Round(kept.Average()),
            P75: PercentileSorted(kept, 75),
            Max: kept[^1],
            SampleCount: kept.Length);
    }

    private static long[] SortedCopy(IReadOnlyList<long> values)
    {
        long[] arr = values.ToArray();
        Array.Sort(arr);
        return arr;
    }

    /// <summary>صدک روی آرایه‌ی <b>از پیش‌مرتب‌شده</b> (بدون مرتب‌سازیِ دوباره).</summary>
    private static long PercentileSorted(long[] sorted, double percentile)
    {
        if (sorted.Length == 0)
        {
            return 0;
        }
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

    /// <summary>برشِ پیوسته‌ی درونِ حصارِ IQR از آرایه‌ی مرتب‌شده؛ با محافظِ بیش‌ازحد-تریم.</summary>
    private static long[] TrimSorted(long[] sorted, int minCountToTrim)
    {
        if (sorted.Length < minCountToTrim)
        {
            return sorted;
        }

        long q1 = PercentileSorted(sorted, 25);
        long q3 = PercentileSorted(sorted, 75);
        long iqr = q3 - q1;
        if (iqr <= 0)
        {
            return sorted; // پراکندگی صفر؛ چیزی برای تریم نیست
        }

        long fence = (long)(1.5 * iqr);
        long lo = q1 - fence, hi = q3 + fence;

        // چون مرتب است، نگه‌داشته‌ها یک برشِ پیوسته‌اند.
        int start = 0;
        while (start < sorted.Length && sorted[start] < lo) start++;
        int end = sorted.Length - 1;
        while (end >= start && sorted[end] > hi) end--;

        int keptCount = end - start + 1;
        // ایمنی: اگر تریم بیش از نیمی را حذف کرد، احتمالاً پراکندگیِ واقعی است؛ خام را نگه دار.
        if (keptCount < Math.Max(3, sorted.Length / 2))
        {
            return sorted;
        }

        return keptCount == sorted.Length ? sorted : sorted[start..(end + 1)];
    }
}
