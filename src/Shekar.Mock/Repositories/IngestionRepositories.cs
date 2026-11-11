using Shekar.Application.Abstractions;
using Shekar.Domain.Entities;
using Shekar.Domain.Enums;

namespace Shekar.Mock.Repositories;

public sealed class MockCarListingRepository : ICarListingRepository
{
    private readonly MockDataStore _store;
    public MockCarListingRepository(MockDataStore store) => _store = store;

    public Task<CarListing?> GetByExternalIdAsync(ListingSource source, string externalId, CancellationToken ct = default) =>
        Task.FromResult(_store.CarListings.FirstOrDefault(l => l.Source == source && l.ExternalId == externalId));

    public Task<bool> ExistsActiveByContentHashAsync(string contentHash, CancellationToken ct = default) =>
        Task.FromResult(_store.CarListings.Any(l => l.State == ListingState.Active && l.ContentHash == contentHash));

    public Task<IReadOnlyList<CarListing>> ListActiveAsync(ListingSource source, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<CarListing>>(
            _store.CarListings.Where(l => l.Source == source && l.State == ListingState.Active).ToList());

    public Task<IReadOnlyList<CarListing>> ListUnmatchedAsync(int max, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<CarListing>>(_store.CarListings
            .Where(l => l.State == ListingState.Active && l.VehicleModelId == null)
            .OrderBy(l => l.Id).Take(max).ToList());

    public Task<IReadOnlyList<CarListing>> ListMatchedActiveSinceAsync(DateTimeOffset sinceUtc, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<CarListing>>(_store.CarListings
            .Where(l => l.State == ListingState.Active && l.VehicleModelId != null && l.PublishedAt >= sinceUtc)
            .ToList());

    public Task<IReadOnlyList<CarListing>> ListMatchedSeenSinceAsync(DateTimeOffset sinceUtc, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<CarListing>>(_store.CarListings
            .Where(l => l.VehicleModelId != null && l.ModelYear != null && l.LastSeenAt >= sinceUtc)
            .ToList());

    public Task<CarListing> AddAsync(CarListing listing, CancellationToken ct = default)
    {
        lock (_store.Gate)
        {
            listing.Id = _store.CarListings.Select(l => l.Id).DefaultIfEmpty(0).Max() + 1;
            _store.CarListings.Add(listing);
        }
        return Task.FromResult(listing);
    }

    public Task UpdateAsync(CarListing listing, CancellationToken ct = default) => Task.CompletedTask;

    public Task<int> CountActiveAsync(CancellationToken ct = default) =>
        Task.FromResult(_store.CarListings.Count(l => l.State == ListingState.Active));

    public Task<int> CountMatchedActiveAsync(CancellationToken ct = default) =>
        Task.FromResult(_store.CarListings.Count(l => l.State == ListingState.Active && l.VehicleModelId != null));

    public Task<int> CountSegmentableActiveAsync(CancellationToken ct = default) =>
        Task.FromResult(_store.CarListings.Count(l =>
            l.State == ListingState.Active && l.VehicleModelId != null && l.ModelYear != null));
}

public sealed class MockVehicleModelRepository : IVehicleModelRepository
{
    private readonly MockDataStore _store;
    public MockVehicleModelRepository(MockDataStore store) => _store = store;

    public Task<VehicleModel?> GetAsync(int id, CancellationToken ct = default) =>
        Task.FromResult(_store.VehicleModels.FirstOrDefault(m => m.Id == id));

    public Task<IReadOnlyList<VehicleModel>> ListActiveAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<VehicleModel>>(_store.VehicleModels.Where(m => m.IsActive).ToList());

    public Task<VehicleModel?> GetByDivarKeyAsync(string divarKey, CancellationToken ct = default) =>
        Task.FromResult(_store.VehicleModels.FirstOrDefault(m =>
            string.Equals(m.DivarKey, divarKey, StringComparison.OrdinalIgnoreCase)));

    public Task<VehicleModel> AddAsync(VehicleModel model, CancellationToken ct = default)
    {
        model.Id = _store.VehicleModels.Count == 0 ? 1 : _store.VehicleModels.Max(m => m.Id) + 1;
        _store.VehicleModels.Add(model);
        return Task.FromResult(model);
    }
}

public sealed class MockMarketSegmentStatRepository : IMarketSegmentStatRepository
{
    private readonly MockDataStore _store;
    public MockMarketSegmentStatRepository(MockDataStore store) => _store = store;

    public Task<MarketSegmentStat?> GetBySegmentKeyAsync(string segmentKey, CancellationToken ct = default) =>
        Task.FromResult(_store.MarketSegmentStats.FirstOrDefault(s => s.SegmentKey == segmentKey));

    public Task<IReadOnlyList<MarketSegmentStat>> ListAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<MarketSegmentStat>>(_store.MarketSegmentStats.ToList());

    public Task UpsertAsync(MarketSegmentStat stat, CancellationToken ct = default)
    {
        lock (_store.Gate)
        {
            MarketSegmentStat? existing = _store.MarketSegmentStats.FirstOrDefault(s => s.SegmentKey == stat.SegmentKey);
            if (existing is null)
            {
                stat.Id = _store.MarketSegmentStats.Select(s => s.Id).DefaultIfEmpty(0).Max() + 1;
                _store.MarketSegmentStats.Add(stat);
            }
            // در صورت وجود، stat همان نمونه‌ی مرجع است و به‌روز شده.
        }
        return Task.CompletedTask;
    }
}

public sealed class MockCrawlRunRepository : ICrawlRunRepository
{
    private readonly MockDataStore _store;
    public MockCrawlRunRepository(MockDataStore store) => _store = store;

    public Task<CrawlRun> AddAsync(CrawlRun run, CancellationToken ct = default)
    {
        lock (_store.Gate)
        {
            run.Id = _store.CrawlRuns.Select(r => r.Id).DefaultIfEmpty(0).Max() + 1;
            _store.CrawlRuns.Add(run);
        }
        return Task.FromResult(run);
    }

    public Task UpdateAsync(CrawlRun run, CancellationToken ct = default) => Task.CompletedTask;

    public Task<IReadOnlyList<CrawlRun>> ListRecentAsync(int take, CancellationToken ct = default)
    {
        lock (_store.Gate)
        {
            IReadOnlyList<CrawlRun> recent = _store.CrawlRuns
                .OrderByDescending(r => r.StartedAt)
                .Take(Math.Max(0, take))
                .ToList();
            return Task.FromResult(recent);
        }
    }
}

public sealed class MockAggregateRunRepository : IAggregateRunRepository
{
    private readonly MockDataStore _store;
    public MockAggregateRunRepository(MockDataStore store) => _store = store;

    public Task<AggregateRun> AddAsync(AggregateRun run, CancellationToken ct = default)
    {
        lock (_store.Gate)
        {
            run.Id = _store.AggregateRuns.Select(r => r.Id).DefaultIfEmpty(0).Max() + 1;
            _store.AggregateRuns.Add(run);
        }
        return Task.FromResult(run);
    }

    public Task UpdateAsync(AggregateRun run, CancellationToken ct = default) => Task.CompletedTask;

    public Task<IReadOnlyList<AggregateRun>> ListRecentAsync(int take, CancellationToken ct = default)
    {
        lock (_store.Gate)
        {
            IReadOnlyList<AggregateRun> recent = _store.AggregateRuns
                .OrderByDescending(r => r.StartedAt)
                .Take(Math.Max(0, take))
                .ToList();
            return Task.FromResult(recent);
        }
    }
}
