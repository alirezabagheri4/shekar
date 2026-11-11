using Shekar.Application.Common;
using Xunit;

namespace Shekar.Application.Tests;

public class StatisticsTests
{
    [Fact]
    public void TrimOutliers_RemovesExtreme_WhenEnoughSamples()
    {
        // ۹ قیمتِ نزدیک + یک پرتِ بزرگ (قیمتِ تایپیِ ۱۰برابری).
        var values = new long[]
        {
            1_000, 1_050, 1_100, 1_020, 1_080, 1_010, 1_060, 1_030, 1_040,
            10_000 // پرت
        };
        var trimmed = Statistics.TrimOutliers(values);
        Assert.DoesNotContain(10_000L, trimmed);
        Assert.Equal(9, trimmed.Count);
    }

    [Fact]
    public void TrimOutliers_KeepsAll_WhenSmallSample()
    {
        // نمونه‌ی کوچک (کمتر از آستانه) → بدون تریم، حتی با پراکندگی زیاد.
        var values = new long[] { 1_000, 1_050, 9_999 };
        var trimmed = Statistics.TrimOutliers(values);
        Assert.Equal(3, trimmed.Count);
    }

    [Fact]
    public void Median_IsRobust_ToOutlier()
    {
        var values = new long[] { 100, 110, 120, 130, 10_000 };
        Assert.Equal(120, Statistics.Median(values));
    }

    [Fact]
    public void Summarize_TrimsOutlier_AndComputesAllStats()
    {
        // ۹ قیمتِ نزدیک + یک پرت؛ Summarize باید پرت را حذف و همه‌ی آماره‌ها را بدهد.
        var values = new long[]
        {
            1_000, 1_050, 1_100, 1_020, 1_080, 1_010, 1_060, 1_030, 1_040,
            10_000 // پرت
        };
        PriceStats s = Statistics.Summarize(values);

        Assert.Equal(9, s.SampleCount);                       // پرت حذف شد
        Assert.Equal(Statistics.Median(new long[] { 1_000, 1_050, 1_100, 1_020, 1_080, 1_010, 1_060, 1_030, 1_040 }), s.Median);
        Assert.True(s.Average is >= 1_000 and <= 1_100);      // میانگینِ مقاوم (نه کشیده به پرت)
        Assert.True(s.P25 <= s.Median && s.Median <= s.P75);  // ترتیبِ چارک‌ها
    }

    [Fact]
    public void Summarize_Empty_ReturnsZeroes()
    {
        PriceStats s = Statistics.Summarize(Array.Empty<long>());
        Assert.Equal(0, s.SampleCount);
        Assert.Equal(0, s.Median);
    }

    [Fact]
    public void Slope_IsNegative_WhenPriceFallsWithMileage()
    {
        // قیمت با افزایشِ کارکرد پایین می‌آید → شیبِ منفی.
        var points = new (double X, double Y)[]
        {
            (10_000, 1_500_000_000), (30_000, 1_450_000_000), (60_000, 1_350_000_000),
            (90_000, 1_250_000_000), (120_000, 1_150_000_000)
        };
        double slope = Statistics.Slope(points);
        Assert.True(slope < 0);
    }

    [Fact]
    public void Slope_IsZero_WhenNoVariationInX()
    {
        var points = new (double X, double Y)[] { (50_000, 1_000), (50_000, 2_000) };
        Assert.Equal(0, Statistics.Slope(points));
    }
}
