using Shekar.Application.Abstractions;
using Shekar.Application.Services;
using Shekar.Domain.Entities;
using Shekar.Domain.Enums;
using Shekar.Mock;
using Shekar.Mock.Repositories;
using Shekar.Mock.Services;
using Xunit;

namespace Shekar.Application.Tests;

public class PipelineHealthTests
{
    private readonly IClock _clock = new TehranClock();
    private readonly MockDataStore _store;

    public PipelineHealthTests()
    {
        _store = new MockDataStore(_clock);
    }

    private PipelineHealthService Build() => new(
        new MockCrawlRunRepository(_store), new MockAggregateRunRepository(_store), new MockCarListingRepository(_store),
        new MockMarketSegmentStatRepository(_store), new MockVehicleModelRepository(_store),
        new MockHuntRepository(_store, _clock), _clock);

    private void AddRecentSuccessfulCrawl(int fetched)
    {
        _store.CrawlRuns.Add(new CrawlRun
        {
            Id = 1, Source = ListingSource.Divar, CityId = 1,
            StartedAt = _clock.UtcNow.AddMinutes(-10), FinishedAt = _clock.UtcNow.AddMinutes(-9),
            Status = CrawlStatus.Succeeded, FetchedCount = fetched, SavedCount = fetched
        });
    }

    private void AddActiveListing(int? vehicleModelId, int? year)
    {
        _store.CarListings.Add(new CarListing
        {
            Id = _store.CarListings.Select(x => x.Id).DefaultIfEmpty(0).Max() + 1,
            Source = ListingSource.Divar, ExternalId = Guid.NewGuid().ToString(), Title = "x",
            State = ListingState.Active, VehicleModelId = vehicleModelId, ModelYear = year,
            PriceToman = 1_000_000_000, CityId = 1,
            PublishedAt = _clock.UtcNow.AddHours(-1), FetchedAt = _clock.UtcNow, LastSeenAt = _clock.UtcNow
        });
    }

    [Fact]
    public async Task Warns_WhenMatchedButNotSegmentable_YearEnrichmentStalled()
    {
        AddRecentSuccessfulCrawl(fetched: 25);
        // ۲۵ آگهیِ نرمال‌شده (مدل دارند) ولی هیچ‌کدام سال ندارند → سگمنت‌پذیر صفر.
        for (int i = 0; i < 25; i++)
        {
            AddActiveListing(vehicleModelId: 1, year: null);
        }

        PipelineHealthReport report = await Build().GetAsync(new PipelineHealthOptions());

        Assert.Equal(25, report.MatchedListings);
        Assert.Equal(0, report.SegmentableListings);
        Assert.Contains(report.Alerts, a => a.Level == HealthLevel.Warning && a.Message.Contains("غنی‌سازی"));
    }

    [Fact]
    public async Task Healthy_WhenCrawlRecentAndListingsSegmentable()
    {
        AddRecentSuccessfulCrawl(fetched: 25);
        for (int i = 0; i < 25; i++)
        {
            AddActiveListing(vehicleModelId: 1, year: 1402);
        }

        PipelineHealthReport report = await Build().GetAsync(new PipelineHealthOptions());

        Assert.Equal(25, report.SegmentableListings);
        Assert.DoesNotContain(report.Alerts, a => a.Message.Contains("غنی‌سازی"));
        Assert.DoesNotContain(report.Alerts, a => a.Level == HealthLevel.Alarm);
    }

    [Fact]
    public async Task Alarms_WhenLastAggregationFailed()
    {
        AddRecentSuccessfulCrawl(fetched: 25);
        for (int i = 0; i < 25; i++)
        {
            AddActiveListing(vehicleModelId: 1, year: 1402);
        }
        _store.AggregateRuns.Add(new AggregateRun
        {
            Id = 1, StartedAt = _clock.UtcNow.AddMinutes(-5), FinishedAt = _clock.UtcNow.AddMinutes(-4),
            Status = CrawlStatus.Failed, Error = "boom"
        });

        PipelineHealthReport report = await Build().GetAsync(new PipelineHealthOptions());

        Assert.Equal(HealthLevel.Alarm, report.Overall);
        Assert.Contains(report.Alerts, a => a.Level == HealthLevel.Alarm && a.Message.Contains("اگریگیشن"));
    }

    [Fact]
    public async Task Alarms_WhenNoCrawlEverRan()
    {
        // بدون هیچ پاسِ کراول → بحرانی.
        PipelineHealthReport report = await Build().GetAsync(new PipelineHealthOptions());
        Assert.Equal(HealthLevel.Alarm, report.Overall);
    }
}
