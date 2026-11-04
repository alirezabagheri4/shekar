using Shekar.Application.Abstractions;
using Shekar.Domain.Entities;

namespace Shekar.Mock.Repositories;

public sealed class MockAdvertiserRepository : IAdvertiserRepository
{
    private readonly MockDataStore _store;
    public MockAdvertiserRepository(MockDataStore store) => _store = store;

    public Task<Advertiser?> GetByPhoneAsync(string phone, CancellationToken ct = default) =>
        Task.FromResult(_store.Advertisers.FirstOrDefault(a => a.Phone == phone));

    public Task<IReadOnlyList<Advertiser>> ListAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Advertiser>>(_store.Advertisers.ToList());

    public Task UpsertAsync(Advertiser advertiser, CancellationToken ct = default)
    {
        lock (_store.Gate)
        {
            if (advertiser.Id == 0)
            {
                advertiser.Id = _store.NextId(_store.Advertisers, a => a.Id);
                _store.Advertisers.Add(advertiser);
            }
        }
        return Task.CompletedTask;
    }
}

public sealed class MockBlockedAdvertiserRepository : IBlockedAdvertiserRepository
{
    private readonly MockDataStore _store;
    public MockBlockedAdvertiserRepository(MockDataStore store) => _store = store;

    public Task<bool> IsBlockedAsync(string phone, DateTimeOffset now, CancellationToken ct = default) =>
        Task.FromResult(_store.BlockedAdvertisers.Any(x =>
            x.Phone == phone && x.IsActive && (x.ExpiresAt == null || x.ExpiresAt > now)));

    public Task<IReadOnlyList<string>> ListActivePhonesAsync(DateTimeOffset now, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<string>>(_store.BlockedAdvertisers
            .Where(x => x.IsActive && (x.ExpiresAt == null || x.ExpiresAt > now))
            .Select(x => x.Phone).Distinct().ToList());

    public Task<IReadOnlyList<BlockedAdvertiser>> ListAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<BlockedAdvertiser>>(
            _store.BlockedAdvertisers.OrderByDescending(x => x.BlockedAt).ToList());

    public Task<BlockedAdvertiser?> GetAsync(int id, CancellationToken ct = default) =>
        Task.FromResult(_store.BlockedAdvertisers.FirstOrDefault(x => x.Id == id));

    public Task<BlockedAdvertiser> AddAsync(BlockedAdvertiser entry, CancellationToken ct = default)
    {
        lock (_store.Gate)
        {
            entry.Id = _store.NextId(_store.BlockedAdvertisers, x => x.Id);
            _store.BlockedAdvertisers.Add(entry);
        }
        return Task.FromResult(entry);
    }

    public Task UpdateAsync(BlockedAdvertiser entry, CancellationToken ct = default) => Task.CompletedTask;
}

public sealed class MockPlanLimitRepository : IPlanLimitRepository
{
    private readonly MockDataStore _store;
    public MockPlanLimitRepository(MockDataStore store) => _store = store;

    public Task<PlanLimit?> GetByPlanAsync(int planId, CancellationToken ct = default) =>
        Task.FromResult(_store.PlanLimits.FirstOrDefault(x => x.PlanId == planId));

    public Task<IReadOnlyList<PlanLimit>> ListAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<PlanLimit>>(_store.PlanLimits.ToList());
}

public sealed class MockSystemSettingRepository : ISystemSettingRepository
{
    private readonly MockDataStore _store;
    public MockSystemSettingRepository(MockDataStore store) => _store = store;

    public Task<SystemSetting?> GetAsync(string key, CancellationToken ct = default) =>
        Task.FromResult(_store.SystemSettings.FirstOrDefault(x => x.Key == key));

    public Task<IReadOnlyList<SystemSetting>> ListAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<SystemSetting>>(_store.SystemSettings.ToList());

    public Task UpsertAsync(SystemSetting setting, CancellationToken ct = default)
    {
        lock (_store.Gate)
        {
            SystemSetting? existing = _store.SystemSettings.FirstOrDefault(x => x.Key == setting.Key);
            if (existing is null)
            {
                _store.SystemSettings.Add(setting);
            }
            else
            {
                existing.Value = setting.Value;
                existing.ValueType = setting.ValueType;
                existing.Description = setting.Description;
                existing.UpdatedAt = setting.UpdatedAt;
            }
        }
        return Task.CompletedTask;
    }
}
