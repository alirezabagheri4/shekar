using Shekar.Application.Abstractions;
using Shekar.Application.Common;
using Shekar.Application.Services;
using Shekar.Domain;
using Shekar.Domain.Entities;
using Shekar.Domain.Enums;
using Shekar.Mock;
using Shekar.Mock.Repositories;
using Shekar.Mock.Services;
using Xunit;

namespace Shekar.Application.Tests;

public class ListingScoringTests
{
    private readonly IClock _clock = new TehranClock();
    private readonly MockDataStore _store;

    public ListingScoringTests()
    {
        _store = new MockDataStore(_clock);
    }

    private ListingScoringService Build() => new(
        new MockCarListingRepository(_store), new MockVehicleModelRepository(_store),
        new MockMarketSegmentStatRepository(_store), new MockHuntRepository(_store, _clock),
        new HuntScorer(), _clock);

    private CarListing AddListing(long price)
    {
        var l = new CarListing
        {
            Id = _store.CarListings.Select(x => x.Id).DefaultIfEmpty(0).Max() + 1,
            Source = ListingSource.Divar, ExternalId = Guid.NewGuid().ToString(), Title = "پژو ۲۰۷ دستی ۱۴۰۲",
            VehicleModelId = 1, ModelYear = 1402, MileageKm = 10_000, BodyCondition = BodyCondition.Healthy,
            PriceToman = price, CityId = 1, State = ListingState.Active,
            PublishedAt = _clock.UtcNow.AddMinutes(-30), FetchedAt = _clock.UtcNow, LastSeenAt = _clock.UtcNow
        };
        _store.CarListings.Add(l);
        return l;
    }

    private void AddSegment(CarListing listing, long median, int samples)
    {
        var model = _store.VehicleModels.First(m => m.Id == 1);
        string key = SegmentKey.ForListing(listing, model)!;
        _store.MarketSegmentStats.Add(new MarketSegmentStat
        {
            Id = _store.MarketSegmentStats.Select(s => s.Id).DefaultIfEmpty(0).Max() + 1,
            SegmentKey = key, VehicleModelId = 1, ModelYear = 1402,
            MileageBucket = MileageBucket.To20k, BodyCondition = BodyCondition.Healthy,
            SampleCount = samples, MedianPriceToman = median, ComputedAt = _clock.UtcNow
        });
    }

    private void AddReliableL2Segment(CarListing listing, long median, int samples)
    {
        var bucket = DomainConstants.ResolveMileageBucket(listing.MileageKm);
        _store.MarketSegmentStats.Add(new MarketSegmentStat
        {
            Id = _store.MarketSegmentStats.Select(s => s.Id).DefaultIfEmpty(0).Max() + 1,
            SegmentKey = SegmentKey.BuildL2(1, listing.ModelYear!.Value, bucket), Level = 2,
            VehicleModelId = 1, ModelYear = listing.ModelYear.Value, MileageBucket = bucket,
            BodyCondition = BodyCondition.Unknown, SampleCount = samples, MedianPriceToman = median,
            IsReliable = true, ComputedAt = _clock.UtcNow
        });
    }

    [Fact]
    public async Task Backoff_UsesCoarserReliableSegment_WhenExactMissing()
    {
        // هیچ سگمنتِ دقیقی (L0) نیست؛ فقط یک سطحِ درشتِ قابل‌اتکا (L2) → باید از آن استفاده شود.
        var listing = AddListing(1_340_000_000);
        AddReliableL2Segment(listing, median: 1_560_000_000, samples: 8);

        ScoringRunResult r = await Build().RunAsync(new ScoringOptions(), windowHours: 48);

        Assert.Equal(1, r.Qualified);
        Assert.Equal(1, r.Created);
        Hunt created = _store.Hunts.Single(h => h.SourceListingId == listing.Id);
        Assert.Equal(1_560_000_000, created.MarketMedianToman);
    }

    [Fact]
    public async Task BelowMarketListing_BecomesHunt()
    {
        // قیمت ۱.۳۴ میلیارد در برابر میانه‌ی ۱.۵۶ میلیارد ≈ ۱۴٪ زیر بازار؛ ۲۰۷ نقدشوندگی ۹۰.
        var listing = AddListing(1_340_000_000);
        AddSegment(listing, median: 1_560_000_000, samples: 8);

        int huntsBefore = _store.Hunts.Count;
        ScoringRunResult r = await Build().RunAsync(new ScoringOptions(), windowHours: 48);

        Assert.Equal(1, r.Evaluated);
        Assert.Equal(1, r.Qualified);
        Assert.Equal(1, r.Created);

        Hunt created = _store.Hunts.Single(h => h.SourceListingId == listing.Id);
        Assert.Equal(14, created.BelowMarketPercent);
        Assert.Equal(1_560_000_000, created.MarketMedianToman);
        Assert.Equal(Liquidity.High, created.Liquidity);
        Assert.True(created.Score >= 60);
        Assert.True(_store.Hunts.Count > huntsBefore);
    }

    [Fact]
    public async Task TooGoodToBeTrue_IsFlaggedSuspicious_NotHunt()
    {
        // ~۵۰٪ زیر بازار → مشکوک (کلاه‌برداری/اقساطی/تایپی)؛ نباید سطح شود.
        var listing = AddListing(780_000_000);
        AddSegment(listing, median: 1_560_000_000, samples: 8);

        ScoringRunResult r = await Build().RunAsync(new ScoringOptions(), windowHours: 48);

        Assert.Equal(1, r.Suspicious);
        Assert.Equal(0, r.Qualified);
        Assert.DoesNotContain(_store.Hunts, h => h.SourceListingId == listing.Id);
    }

    [Fact]
    public async Task DealerReposts_AreDeduplicated()
    {
        // دو آگهیِ نزدیک‌تکرار (هم‌مدل/سال/قیمت/محله) → فقط یک شکار.
        var a = AddListing(1_340_000_000);
        var b = AddListing(1_340_000_000);
        AddSegment(a, median: 1_560_000_000, samples: 8);

        ScoringRunResult r = await Build().RunAsync(new ScoringOptions(), windowHours: 48);

        Assert.Equal(2, r.Qualified);
        Assert.Equal(1, r.Created);
        Assert.Equal(1, r.Duplicates);
    }

    [Fact]
    public async Task FairlyPricedListing_DoesNotQualify()
    {
        // قیمت نزدیک میانه → زیر بازار ناچیز → واجد شرایط نیست.
        var listing = AddListing(1_540_000_000);
        AddSegment(listing, median: 1_560_000_000, samples: 8);

        ScoringRunResult r = await Build().RunAsync(new ScoringOptions(), windowHours: 48);

        Assert.Equal(1, r.Evaluated);
        Assert.Equal(0, r.Qualified);
        Assert.DoesNotContain(_store.Hunts, h => h.SourceListingId == listing.Id);
    }
}
