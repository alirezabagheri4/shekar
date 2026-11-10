using Shekar.Application.Abstractions;
using Shekar.Application.Contracts;
using Shekar.Application.Services;
using Shekar.Domain.Entities;
using Shekar.Domain.Enums;
using Shekar.Mock;
using Shekar.Mock.Repositories;
using Shekar.Mock.Services;
using Xunit;

namespace Shekar.Application.Tests;

public class ListingIngestionTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 31, 9, 0, 0, TimeSpan.Zero);
    private readonly IClock _clock = new TehranClock();

    private sealed class StubProvider : IListingSourceProvider
    {
        private readonly IReadOnlyList<RawListingDto> _ads;
        public StubProvider(IReadOnlyList<RawListingDto> ads) => _ads = ads;
        public ListingSource Source => ListingSource.Divar;
        public Task<IReadOnlyList<RawListingDto>> FetchRecentAsync(int cityId, CancellationToken ct = default) =>
            Task.FromResult(_ads);
    }

    private RawListingDto Ad(string id, long price = 1_000_000_000, int images = 3,
        double hoursAgo = 1, string phone = "09120000001", string title = "پژو ۲۰۷ دستی") => new()
    {
        Source = ListingSource.Divar,
        ExternalId = id,
        Title = title,
        PriceToman = price,
        ImageCount = images,
        AdvertiserPhone = phone,
        CityId = 1,
        PublishedAt = _clock.UtcNow.AddHours(-hoursAgo)
    };

    [Fact]
    public async Task Ingestion_AppliesAllFilters()
    {
        var store = new MockDataStore(_clock);
        var listings = new MockCarListingRepository(store);
        var runs = new MockCrawlRunRepository(store);
        var blocked = new MockBlockedAdvertiserRepository(store);

        // یک شماره را بلاک می‌کنیم.
        store.BlockedAdvertisers.Add(new BlockedAdvertiser
        {
            Id = 50, Phone = "09128888888", Reason = BlockReason.TooManyAds,
            BlockedAt = _clock.UtcNow, IsActive = true
        });

        // آگهی موجود برای تست تکراریِ دقیق.
        await listings.AddAsync(new CarListing
        {
            Source = ListingSource.Divar, ExternalId = "DUP-1", Title = "موجود",
            PriceToman = 900_000_000, CityId = 1, State = ListingState.Active,
            PublishedAt = _clock.UtcNow, FetchedAt = _clock.UtcNow, LastSeenAt = _clock.UtcNow
        });

        var ads = new List<RawListingDto>
        {
            Ad("OK-1"),                                   // معتبر → ذخیره
            Ad("OLD-1", hoursAgo: 48),                    // قدیمی‌تر از ۲۴ ساعت → رد
            Ad("NOPIC-1", images: 0),                     // بدون عکس → رد
            Ad("NOPRICE-1", price: 0),                    // بدون قیمت → رد
            Ad("BLOCK-1", phone: "09128888888"),          // شماره بلاک‌شده → رد
            Ad("DUP-1"),                                  // تکراری دقیق → به‌روزرسانی (نه ذخیره، نه رد)
            Ad("NEAR-1", phone: "09120000001", title: "پژو ۲۰۷ دستی") // تکراری نزدیکِ OK-1 → رد
        };

        var svc = new ListingIngestionService(listings, runs, blocked, _clock);
        IngestionResult r = await svc.RunAsync(new StubProvider(ads), cityId: 1, maxAgeHours: 24, staleAfterHours: 72);

        Assert.Equal(7, r.Fetched);
        Assert.Equal(1, r.Saved);     // فقط OK-1
        Assert.Equal(5, r.Skipped);   // OLD, NOPIC, NOPRICE, BLOCK, NEAR
        Assert.Equal(0, r.Errors);

        // یک CrawlRun موفق ثبت شده.
        Assert.Single(store.CrawlRuns);
        Assert.Equal(CrawlStatus.Succeeded, store.CrawlRuns[0].Status);
    }
}
