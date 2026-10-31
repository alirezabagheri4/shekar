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
    public async Task Aggregate_SplitsByColor_WhenAbove4Billion()
    {
        // بالای ۴ میلیارد → رنگ در کلید سگمنت دخیل می‌شود.
        AddListing(1, 1403, 5_000, 5_000_000_000, BodyCondition.Healthy, "سفید");
        AddListing(1, 1403, 6_000, 5_200_000_000, BodyCondition.Healthy, "مشکی");

        int segments = await Build().RunAsync(windowDays: 30);

        Assert.Equal(2, segments); // دو رنگ → دو سگمنت
        Assert.All(_store.MarketSegmentStats, s => Assert.NotNull(s.ColorKey));
    }
}
