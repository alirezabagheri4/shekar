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
}
