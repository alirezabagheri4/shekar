using Shekar.Application.Abstractions;
using Shekar.Application.Services;
using Shekar.Domain.Entities;
using Shekar.Domain.Enums;
using Shekar.Mock;
using Shekar.Mock.Repositories;
using Shekar.Mock.Services;
using Xunit;

namespace Shekar.Application.Tests;

public class MarketAggregationTests
{
    private readonly IClock _clock = new TehranClock();
    private readonly MockDataStore _store;

    public MarketAggregationTests()
    {
        _store = new MockDataStore(_clock);
    }

    private MarketAggregationService Build() => new(
        new MockCarListingRepository(_store), new MockVehicleModelRepository(_store),
        new MockMarketSegmentStatRepository(_store), _clock);

    private void AddListing(int vmId, int year, int? mileage, long price, BodyCondition body, string? color)
    {
        _store.CarListings.Add(new CarListing
        {
            Id = _store.CarListings.Select(l => l.Id).DefaultIfEmpty(0).Max() + 1,
            Source = ListingSource.Divar, ExternalId = Guid.NewGuid().ToString(),
            Title = "x", VehicleModelId = vmId, ModelYear = year, MileageKm = mileage,
            PriceToman = price, BodyCondition = body, Color = color,
            State = ListingState.Active, PublishedAt = _clock.UtcNow.AddHours(-1),
            FetchedAt = _clock.UtcNow, LastSeenAt = _clock.UtcNow
        });
    }

    [Fact]
    public async Task Aggregate_ComputesMedian_ForSegment()
    {
        // سه آگهیِ هم‌سگمنت (مدل ۱، سال ۱۴۰۲، کارکرد ~۱۰هزار، سالم، زیر ۴ میلیارد).
        AddListing(1, 1402, 10_000, 1_000_000_000, BodyCondition.Healthy, "سفید");
        AddListing(1, 1402, 12_000, 1_200_000_000, BodyCondition.Healthy, "مشکی"); // رنگ متفاوت ولی زیر ۴ میلیارد → همان سگمنت
        AddListing(1, 1402, 18_000, 1_400_000_000, BodyCondition.Healthy, "سفید");

        int segments = await Build().RunAsync(windowDays: 30);

        Assert.Equal(1, segments); // رنگ در سگمنت دخیل نیست → یک سگمنت
        var stat = _store.MarketSegmentStats.Single();
        Assert.Equal(3, stat.SampleCount);
        Assert.Equal(1_200_000_000, stat.MedianPriceToman); // میانه‌ی ۱.۰/۱.۲/۱.۴
        Assert.Null(stat.ColorKey);
    }

    [Fact]
    public async Task Aggregate_IncludesRecentlyRemoved_ForHistory()
    {
        // آگهیِ فعال + آگهیِ به‌تازگی‌حذف‌شده (هم‌سگمنت) باید هر دو در نمونه بیایند.
        AddListing(1, 1402, 10_000, 1_000_000_000, BodyCondition.Healthy, "سفید");
        _store.CarListings.Add(new CarListing
        {
            Id = _store.CarListings.Select(l => l.Id).Max() + 1,
            Source = ListingSource.Divar, ExternalId = Guid.NewGuid().ToString(),
            Title = "x", VehicleModelId = 1, ModelYear = 1402, MileageKm = 11_000,
            PriceToman = 1_400_000_000, BodyCondition = BodyCondition.Healthy, Color = "مشکی",
            State = ListingState.Removed, PublishedAt = _clock.UtcNow.AddHours(-20),
            FetchedAt = _clock.UtcNow.AddHours(-20), LastSeenAt = _clock.UtcNow.AddHours(-2),
            RemovedAt = _clock.UtcNow.AddHours(-2)
        });

        int segments = await Build().RunAsync(windowDays: 30);

        Assert.Equal(1, segments);
        Assert.Equal(2, _store.MarketSegmentStats.Single().SampleCount); // شاملِ حذف‌شده‌ی اخیر
    }

    [Fact]
    public async Task Aggregate_ComputesNegativeMileageSlope_AtModelYearLevel()
    {
        // قیمت با کارکرد پایین می‌آید؛ چند باکتِ کارکرد → سطحِ L3 با شیبِ منفی و کارکردِ مرجع.
        AddListing(1, 1402, 10_000, 1_500_000_000, BodyCondition.Healthy, null);
        AddListing(1, 1402, 30_000, 1_450_000_000, BodyCondition.Healthy, null);
        AddListing(1, 1402, 60_000, 1_350_000_000, BodyCondition.Healthy, null);
        AddListing(1, 1402, 90_000, 1_250_000_000, BodyCondition.Healthy, null);
        AddListing(1, 1402, 120_000, 1_150_000_000, BodyCondition.Healthy, null);
        AddListing(1, 1402, 150_000, 1_050_000_000, BodyCondition.Healthy, null);

        await Build().RunAsync(windowDays: 30, new AggregationOptions());

        var l3 = _store.MarketSegmentStats.Single(s => s.Level == 3);
        Assert.StartsWith("L3|", l3.SegmentKey);
        Assert.True(l3.MileageSlopeTomanPerK < 0);  // قیمت با کارکرد نزول می‌کند
        Assert.True(l3.RefMileageKm > 0);           // کارکردِ مرجع (میانه) تنظیم شد
    }

    [Fact]
    public async Task Aggregate_BuildsCoarseLevel_WhenMergingBodies()
    {
        // دو بدنه‌ی متفاوت در یک (مدل+سال+کارکرد) → سطحِ L2 (بدونِ بدنه) باید ساخته شود و بدنه‌ها را ادغام کند.
        for (int i = 0; i < 3; i++)
        {
            AddListing(1, 1402, 10_000, 1_000_000_000 + i * 10_000_000, BodyCondition.Healthy, null);
            AddListing(1, 1402, 12_000, 1_020_000_000 + i * 10_000_000, BodyCondition.Painted, null);
        }

        await Build().RunAsync(windowDays: 30, new AggregationOptions());

        // دو سگمنتِ L0 (هر بدنه) + یک سگمنتِ L2 (ادغامِ بدنه‌ها).
        Assert.Equal(2, _store.MarketSegmentStats.Count(s => s.Level == 0));
        Assert.Equal(0, _store.MarketSegmentStats.Count(s => s.Level == 1)); // رنگ دخیل نیست → L1 افزونه نیست
        var l2 = _store.MarketSegmentStats.Single(s => s.Level == 2);
        Assert.StartsWith("L2|", l2.SegmentKey);
        Assert.Equal(6, l2.RawSampleCount); // هر دو بدنه
    }

    [Fact]
    public async Task Aggregate_SkipsRedundantCoarse_WhenSingleChild()
    {
        // همه هم‌بدنه/هم‌رنگ → هیچ ادغامی لازم نیست؛ فقط L0.
        for (int i = 0; i < 4; i++)
        {
            AddListing(1, 1402, 10_000, 1_000_000_000 + i * 10_000_000, BodyCondition.Healthy, null);
        }

        await Build().RunAsync(windowDays: 30, new AggregationOptions());

        Assert.Single(_store.MarketSegmentStats);
        Assert.Equal(0, _store.MarketSegmentStats.Single().Level);
    }

    [Fact]
    public async Task Aggregate_MarksReliable_AndFillsQualityFields()
    {
        // ۶ آگهیِ هم‌سگمنت با پراکندگیِ کم → سگمنتِ قابل‌اتکا.
        for (int i = 0; i < 6; i++)
        {
            AddListing(1, 1402, 10_000, 1_000_000_000 + i * 10_000_000, BodyCondition.Healthy, "سفید");
        }

        AggregationResult r = await Build().RunAsync(windowDays: 30, new AggregationOptions());

        Assert.Equal(1, r.Segments);
        Assert.Equal(1, r.Reliable);
        var stat = _store.MarketSegmentStats.Single();
        Assert.True(stat.IsReliable);
        Assert.Equal(6, stat.RawSampleCount);
        Assert.True(stat.MinPriceToman > 0);
        Assert.True(stat.SpreadPercent is >= 0 and <= 60);
    }

    [Fact]
    public async Task Aggregate_MarksLowSample_AsNotReliable()
    {
        // فقط ۲ نمونه → زیرِ آستانه‌ی ۵ → غیرقابل‌اتکا (ولی سگمنت ساخته می‌شود).
        AddListing(1, 1402, 10_000, 1_000_000_000, BodyCondition.Healthy, "سفید");
        AddListing(1, 1402, 12_000, 1_050_000_000, BodyCondition.Healthy, "سفید");

        AggregationResult r = await Build().RunAsync(windowDays: 30, new AggregationOptions());

        Assert.Equal(1, r.Segments);
        Assert.Equal(0, r.Reliable);
        Assert.False(_store.MarketSegmentStats.Single().IsReliable);
    }

    [Fact]
    public async Task Aggregate_StalesSegment_WhenNoLongerSeen()
    {
        for (int i = 0; i < 6; i++)
        {
            AddListing(1, 1402, 10_000, 1_000_000_000 + i * 10_000_000, BodyCondition.Healthy, "سفید");
        }
        await Build().RunAsync(windowDays: 30, new AggregationOptions());
        Assert.True(_store.MarketSegmentStats.Single().IsReliable);

        // همه‌ی آگهی‌ها از پنجره خارج شوند (دیگر دیده نمی‌شوند).
        foreach (var l in _store.CarListings)
        {
            l.LastSeenAt = _clock.UtcNow.AddDays(-40);
        }

        AggregationResult r = await Build().RunAsync(windowDays: 30, new AggregationOptions());

        Assert.Equal(0, r.Segments);  // هیچ سگمنتِ تازه‌ای
        Assert.Equal(1, r.Staled);    // سگمنتِ قبلی کهنه شد
        Assert.False(_store.MarketSegmentStats.Single().IsReliable);
    }

    [Fact]
    public async Task Aggregate_SplitsByColor_WhenAbove4Billion()
    {
        // بالای ۴ میلیارد → رنگ در کلید سگمنتِ دقیق دخیل می‌شود.
        AddListing(1, 1403, 5_000, 5_000_000_000, BodyCondition.Healthy, "سفید");
        AddListing(1, 1403, 6_000, 5_200_000_000, BodyCondition.Healthy, "مشکی");

        await Build().RunAsync(windowDays: 30, new AggregationOptions());

        // دو رنگ → دو سگمنتِ دقیق (L0)، هرکدام با رنگِ خودش.
        var level0 = _store.MarketSegmentStats.Where(s => s.Level == 0).ToList();
        Assert.Equal(2, level0.Count);
        Assert.All(level0, s => Assert.NotNull(s.ColorKey));
        // و یک سطحِ L1 که دو رنگ را ادغام می‌کند (بدونِ رنگ).
        var l1 = _store.MarketSegmentStats.Single(s => s.Level == 1);
        Assert.Null(l1.ColorKey);
        Assert.Equal(2, l1.RawSampleCount);
    }
}
