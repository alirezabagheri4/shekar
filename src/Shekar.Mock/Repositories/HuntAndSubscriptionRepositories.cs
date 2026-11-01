using Shekar.Application.Abstractions;
using Shekar.Domain;
using Shekar.Domain.Entities;
using Shekar.Domain.Enums;
using Shekar.Mock.Services;

namespace Shekar.Mock.Repositories;

public sealed class MockSubscriptionRepository : ISubscriptionRepository
{
    private readonly MockDataStore _store;
    public MockSubscriptionRepository(MockDataStore store) => _store = store;

    public Task<Subscription?> GetActiveForUserAsync(int userId, DateTimeOffset now, CancellationToken ct = default) =>
        Task.FromResult(_store.Subscriptions
            .Where(s => s.UserId == userId && s.Status != SubscriptionStatus.Expired && s.ExpiresAt > now)
            .OrderByDescending(s => s.ExpiresAt)
            .FirstOrDefault());

    public Task<int> CountActiveInCityAsync(int cityId, DateTimeOffset now, CancellationToken ct = default) =>
        Task.FromResult(_store.Subscriptions.Count(s =>
            s.CityId == cityId
            && (s.Status == SubscriptionStatus.Trial || s.Status == SubscriptionStatus.Active)
            && s.ExpiresAt > now));

    public Task<Subscription> AddAsync(Subscription subscription, CancellationToken ct = default)
    {
        lock (_store.Gate)
        {
            subscription.Id = _store.NextId(_store.Subscriptions, s => s.Id);
            _store.Subscriptions.Add(subscription);
        }
        return Task.FromResult(subscription);
    }
}

public sealed class MockHuntRepository : IHuntRepository
{
    private readonly MockDataStore _store;
    private readonly IClock _clock;

    public MockHuntRepository(MockDataStore store, IClock clock)
    {
        _store = store;
        _clock = clock;
    }

    public Task<IReadOnlyList<Hunt>> QueryAsync(HuntQuery query, CancellationToken ct = default)
    {
        DateTime target = _clock.TehranNow.Date.AddDays(-(int)query.Day);
        HashSet<string> blocked = ActiveBlockedPhones();

        IEnumerable<Hunt> result = _store.Hunts.Where(h =>
            h.CityId == query.CityId
            && TehranTime.ToTehran(h.PublishedAt).Date == target
            && !IsBlocked(h, blocked));

        if (query.Category is { } category)
        {
            result = result.Where(h => DomainConstants.ResolveCategory(h.PriceToman) == category);
        }

        return Task.FromResult<IReadOnlyList<Hunt>>(result.ToList());
    }

    public Task<IReadOnlyList<Hunt>> ListTodayAsync(int cityId, CancellationToken ct = default)
    {
        DateTime today = _clock.TehranNow.Date;
        HashSet<string> blocked = ActiveBlockedPhones();
        return Task.FromResult<IReadOnlyList<Hunt>>(_store.Hunts
            .Where(h => h.CityId == cityId && TehranTime.ToTehran(h.PublishedAt).Date == today && !IsBlocked(h, blocked))
            .ToList());
    }

    // شماره‌های بلاک فعال؛ آگهی‌هایشان از پیشنهادهای روزانه حذف می‌شود (ضد دلال/اسپم).
    private HashSet<string> ActiveBlockedPhones()
    {
        DateTimeOffset now = _clock.UtcNow;
        return _store.BlockedAdvertisers
            .Where(b => b.IsActive && (b.ExpiresAt == null || b.ExpiresAt > now))
            .Select(b => b.Phone)
            .ToHashSet();
    }

    private static bool IsBlocked(Hunt h, HashSet<string> blocked) =>
        h.AdvertiserPhone is { } phone && blocked.Contains(phone);

    public Task<Hunt?> GetAsync(int id, CancellationToken ct = default) =>
        Task.FromResult(_store.Hunts.FirstOrDefault(h => h.Id == id));

    public Task<IReadOnlyList<Hunt>> ListPipelineAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Hunt>>(_store.Hunts.Where(h => h.SourceListingId != null).ToList());

    public Task<Hunt> AddAsync(Hunt hunt, CancellationToken ct = default)
    {
        lock (_store.Gate)
        {
            hunt.Id = _store.Hunts.Select(h => h.Id).DefaultIfEmpty(0).Max() + 1;
            _store.Hunts.Add(hunt);
        }
        return Task.FromResult(hunt);
    }

    public Task UpdateAsync(Hunt hunt, CancellationToken ct = default) => Task.CompletedTask;
}
