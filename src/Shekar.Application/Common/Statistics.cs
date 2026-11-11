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
    /// شیبِ رگرسیونِ خطی (کمترین‌مربعات) y بر حسب x؛ <c>0</c> اگر کمتر از ۲ نقطه یا x بدونِ پراکندگی.
    /// برای برآوردِ «افتِ قیمت به‌ازای هر واحدِ کارکرد».
    /// </summary>
    public static double Slope(IReadOnlyList<(double X, double Y)> points)
    {
        if (points.Count < 2)
        {
            return 0;
        }

        double meanX = points.Average(p => p.X);
        double meanY = points.Average(p => p.Y);
        double sxx = 0, sxy = 0;
        foreach ((double x, double y) in points)
        {
            double dx = x - meanX;
            sxx += dx * dx;
            sxy += dx * (y - meanY);
        }
        return sxx <= 0 ? 0 : sxy / sxx;
    }

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

    /// <summary>
    /// مثلِ <see cref="Summarize"/> ولی با وزنِ هر نمونه (برای وزن‌دهیِ زمانی): پرت‌ها با IQR
    /// (بدونِ وزن، روی قیمت) کنار می‌روند، سپس میانه/چارک‌ها به‌صورتِ «وزنیِ پله‌ای» و میانگینِ وزنی
    /// حساب می‌شوند. با وزنِ یکسان، به میانه‌ی معمول تقلیل می‌یابد.
    /// </summary>
    public static PriceStats WeightedSummarize(IReadOnlyList<(long Price, double Weight)> pairs, int minCountToTrim = 8)
    {
        if (pairs.Count == 0)
        {
            return new PriceStats(0, 0, 0, 0, 0, 0, 0);
        }

        (long Price, double Weight)[] sorted = pairs.OrderBy(p => p.Price).ToArray();
        (int start, int end) = TrimBounds(sorted, minCountToTrim);

        double total = 0, weightedSum = 0;
        for (int i = start; i <= end; i++)
        {
            total += sorted[i].Weight;
            weightedSum += sorted[i].Weight * sorted[i].Price;
        }

        long mean = total > 0 ? (long)Math.Round(weightedSum / total) : sorted[start].Price;
        return new PriceStats(
            Min: sorted[start].Price,
            P25: WeightedPercentile(sorted, start, end, total, 25),
            Median: WeightedPercentile(sorted, start, end, total, 50),
            Average: mean,
            P75: WeightedPercentile(sorted, start, end, total, 75),
            Max: sorted[end].Price,
            SampleCount: end - start + 1);
    }

    /// <summary>کوچک‌ترین قیمتی که وزنِ تجمعی‌اش به <paramref name="percentile"/>٪ از کلِ وزن برسد.</summary>
    private static long WeightedPercentile((long Price, double Weight)[] sorted, int start, int end, double total, double percentile)
    {
        if (total <= 0)
        {
            return sorted[start].Price;
        }
        double target = percentile / 100.0 * total;
        double cum = 0;
        for (int i = start; i <= end; i++)
        {
            cum += sorted[i].Weight;
            if (cum >= target)
            {
                return sorted[i].Price;
            }
        }
        return sorted[end].Price;
    }

    /// <summary>مرزهای برشِ IQR (۱.۵×) روی آرایه‌ی مرتب‌شده‌ی زوج‌ها؛ با محافظِ بیش‌ازحد-تریم.</summary>
    private static (int Start, int End) TrimBounds((long Price, double Weight)[] sorted, int minCountToTrim)
    {
        int n = sorted.Length;
        if (n < minCountToTrim)
        {
            return (0, n - 1);
        }

        long[] prices = new long[n];
        for (int i = 0; i < n; i++)
        {
            prices[i] = sorted[i].Price;
        }

        long q1 = PercentileSorted(prices, 25), q3 = PercentileSorted(prices, 75);
        long iqr = q3 - q1;
        if (iqr <= 0)
        {
            return (0, n - 1);
        }

        long fence = (long)(1.5 * iqr);
        long lo = q1 - fence, hi = q3 + fence;
        int start = 0;
        while (start < n && sorted[start].Price < lo) start++;
        int end = n - 1;
        while (end >= start && sorted[end].Price > hi) end--;

        int kept = end - start + 1;
        return kept < Math.Max(3, n / 2) ? (0, n - 1) : (start, end);
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
