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
}
