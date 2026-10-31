using Microsoft.EntityFrameworkCore;
using Shekar.Application.Abstractions;
using Shekar.Domain;
using Shekar.Domain.Entities;
using Shekar.Domain.Enums;
using Shekar.Infrastructure.Persistence;

namespace Shekar.Infrastructure.Repositories;

public sealed class EfCityRepository : ICityRepository
{
    private readonly ShekarDbContext _db;
    public EfCityRepository(ShekarDbContext db) => _db = db;

    public Task<City?> GetAsync(int id, CancellationToken ct = default) =>
        _db.Cities.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<City?> GetBySlugAsync(string slug, CancellationToken ct = default) =>
        _db.Cities.FirstOrDefaultAsync(c => c.Slug == slug, ct);

    public async Task<IReadOnlyList<City>> ListAsync(CancellationToken ct = default) =>
        await _db.Cities.AsNoTracking().OrderBy(c => c.Id).ToListAsync(ct);
}

public sealed class EfPlanRepository : IPlanRepository
{
    private readonly ShekarDbContext _db;
    public EfPlanRepository(ShekarDbContext db) => _db = db;

    public async Task<IReadOnlyList<Plan>> ListAsync(CancellationToken ct = default) =>
        await _db.Plans.AsNoTracking().OrderBy(p => p.Type).ToListAsync(ct);

    public Task<Plan?> GetAsync(PlanType type, CancellationToken ct = default) =>
        _db.Plans.FirstOrDefaultAsync(p => p.Type == type, ct);
}

public sealed class EfUserRepository : IUserRepository
{
    private readonly ShekarDbContext _db;
    public EfUserRepository(ShekarDbContext db) => _db = db;

    public Task<User?> GetAsync(int id, CancellationToken ct = default) =>
        _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<User?> GetByMobileAsync(string mobile, CancellationToken ct = default) =>
        _db.Users.FirstOrDefaultAsync(u => u.Mobile == mobile, ct);

    public async Task<User> AddAsync(User user, CancellationToken ct = default)
    {
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
        return user;
    }

    public async Task UpdateAsync(User user, CancellationToken ct = default)
    {
        _db.Users.Update(user);
        await _db.SaveChangesAsync(ct);
    }
}

public sealed class EfSubscriptionRepository : ISubscriptionRepository
{
    private readonly ShekarDbContext _db;
    public EfSubscriptionRepository(ShekarDbContext db) => _db = db;

    public Task<Subscription?> GetActiveForUserAsync(int userId, DateTimeOffset now, CancellationToken ct = default) =>
        _db.Subscriptions
            .Where(s => s.UserId == userId && s.Status != SubscriptionStatus.Expired && s.ExpiresAt > now)
            .OrderByDescending(s => s.ExpiresAt)
            .FirstOrDefaultAsync(ct);

    public Task<int> CountActiveInCityAsync(int cityId, DateTimeOffset now, CancellationToken ct = default) =>
        _db.Subscriptions.CountAsync(s =>
            s.CityId == cityId
            && (s.Status == SubscriptionStatus.Trial || s.Status == SubscriptionStatus.Active)
            && s.ExpiresAt > now, ct);

    public async Task<Subscription> AddAsync(Subscription subscription, CancellationToken ct = default)
    {
        _db.Subscriptions.Add(subscription);
        await _db.SaveChangesAsync(ct);
        return subscription;
    }
}

public sealed class EfHuntRepository : IHuntRepository
{
    private readonly ShekarDbContext _db;
    private readonly IClock _clock;

    public EfHuntRepository(ShekarDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<IReadOnlyList<Hunt>> QueryAsync(HuntQuery query, CancellationToken ct = default)
    {
        (DateTimeOffset start, DateTimeOffset next) = DayRangeUtc(query.Day);
        IQueryable<Hunt> q = BaseDailyQuery(query.CityId, start, next);

        if (query.Category is { } category)
        {
            q = q.Where(h => h.PriceToman >= Lower(category) && (Upper(category) == null || h.PriceToman < Upper(category)));
        }

        return await q.AsNoTracking().ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Hunt>> ListTodayAsync(int cityId, CancellationToken ct = default)
    {
        (DateTimeOffset start, DateTimeOffset next) = DayRangeUtc(HuntDay.Today);
        return await BaseDailyQuery(cityId, start, next).AsNoTracking().ToListAsync(ct);
    }

    public Task<Hunt?> GetAsync(int id, CancellationToken ct = default) =>
        _db.Hunts
            .Include(h => h.Analysis).ThenInclude(a => a!.Factors)
            .FirstOrDefaultAsync(h => h.Id == id, ct);

    /// <summary>کوئری پایه‌ی لیست روزانه با حذف شماره‌های بلاک‌شده (ضد دلال/اسپم).</summary>
    private IQueryable<Hunt> BaseDailyQuery(int cityId, DateTimeOffset start, DateTimeOffset next)
    {
        DateTimeOffset now = _clock.UtcNow;
        IQueryable<string> blocked = _db.BlockedAdvertisers
            .Where(x => x.IsActive && (x.ExpiresAt == null || x.ExpiresAt > now))
            .Select(x => x.Phone);

        return _db.Hunts.Where(h =>
            h.CityId == cityId
            && h.PublishedAt >= start && h.PublishedAt < next
            && (h.AdvertiserPhone == null || !blocked.Contains(h.AdvertiserPhone)));
    }

    private (DateTimeOffset Start, DateTimeOffset Next) DayRangeUtc(HuntDay day)
    {
        // محدوده‌ی روزِ تهران (با افست جاری) را به UTC تبدیل می‌کند.
        DateTimeOffset tehranNow = _clock.TehranNow;
        TimeSpan offset = tehranNow.Offset;
        DateTime targetLocalDate = tehranNow.Date.AddDays(-(int)day);
        var start = new DateTimeOffset(targetLocalDate, offset);
        return (start.ToUniversalTime(), start.AddDays(1).ToUniversalTime());
    }

    private static long Lower(PriceCategory c) =>
        DomainConstants.PriceCategoryBands.First(x => x.Category == c).MinToman;

    private static long? Upper(PriceCategory c) =>
        DomainConstants.PriceCategoryBands.First(x => x.Category == c).MaxTomanExclusive;
}

public sealed class EfAppraisalRepository : IAppraisalRepository
{
    private readonly ShekarDbContext _db;
    public EfAppraisalRepository(ShekarDbContext db) => _db = db;

    public Task<int> CountForUserInRangeAsync(int userId, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken ct = default) =>
        _db.Appraisals.CountAsync(a => a.UserId == userId && a.CreatedAt >= fromUtc && a.CreatedAt < toUtc, ct);

    public async Task<IReadOnlyList<Appraisal>> ListForUserAsync(int userId, CancellationToken ct = default) =>
        await _db.Appraisals.AsNoTracking().Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedAt).ToListAsync(ct);

    public async Task<Appraisal> AddAsync(Appraisal appraisal, CancellationToken ct = default)
    {
        _db.Appraisals.Add(appraisal);
        await _db.SaveChangesAsync(ct);
        return appraisal;
    }
}

public sealed class EfBellAlertRepository : IBellAlertRepository
{
    private readonly ShekarDbContext _db;
    public EfBellAlertRepository(ShekarDbContext db) => _db = db;

    public async Task<IReadOnlyList<BellAlert>> ListForUserAsync(int userId, CancellationToken ct = default) =>
        await _db.BellAlerts.Where(x => x.UserId == userId).ToListAsync(ct);

    public Task<BellAlert?> GetAsync(int userId, PriceCategory category, CancellationToken ct = default) =>
        _db.BellAlerts.FirstOrDefaultAsync(x => x.UserId == userId && x.Category == category, ct);

    public async Task UpsertAsync(BellAlert alert, CancellationToken ct = default)
    {
        if (alert.Id == 0)
        {
            _db.BellAlerts.Add(alert);
        }
        else
        {
            _db.BellAlerts.Update(alert);
        }
        await _db.SaveChangesAsync(ct);
    }
}

public sealed class EfWaitlistRepository : IWaitlistRepository
{
    private readonly ShekarDbContext _db;
    public EfWaitlistRepository(ShekarDbContext db) => _db = db;

    public Task<WaitlistEntry?> GetAsync(int cityId, string mobile, CancellationToken ct = default) =>
        _db.WaitlistEntries.FirstOrDefaultAsync(w => w.CityId == cityId && w.Mobile == mobile, ct);

    public Task<int> CountForCityAsync(int cityId, CancellationToken ct = default) =>
        _db.WaitlistEntries.CountAsync(w => w.CityId == cityId, ct);

    public async Task<WaitlistEntry> AddAsync(WaitlistEntry entry, CancellationToken ct = default)
    {
        _db.WaitlistEntries.Add(entry);
        await _db.SaveChangesAsync(ct);
        return entry;
    }
}

public sealed class EfOtpRepository : IOtpRepository
{
    private readonly ShekarDbContext _db;
    public EfOtpRepository(ShekarDbContext db) => _db = db;

    public Task<OtpCode?> GetLatestAsync(string mobile, CancellationToken ct = default) =>
        _db.OtpCodes.Where(o => o.Mobile == mobile).OrderByDescending(o => o.CreatedAt).FirstOrDefaultAsync(ct);

    public async Task AddAsync(OtpCode code, CancellationToken ct = default)
    {
        _db.OtpCodes.Add(code);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(OtpCode code, CancellationToken ct = default)
    {
        _db.OtpCodes.Update(code);
        await _db.SaveChangesAsync(ct);
    }
}

public sealed class EfAdvertiserRepository : IAdvertiserRepository
{
    private readonly ShekarDbContext _db;
    public EfAdvertiserRepository(ShekarDbContext db) => _db = db;

    public Task<Advertiser?> GetByPhoneAsync(string phone, CancellationToken ct = default) =>
        _db.Advertisers.FirstOrDefaultAsync(a => a.Phone == phone, ct);

    public async Task<IReadOnlyList<Advertiser>> ListAsync(CancellationToken ct = default) =>
        await _db.Advertisers.AsNoTracking().ToListAsync(ct);

    public async Task UpsertAsync(Advertiser advertiser, CancellationToken ct = default)
    {
        if (advertiser.Id == 0)
        {
            _db.Advertisers.Add(advertiser);
        }
        else
        {
            _db.Advertisers.Update(advertiser);
        }
        await _db.SaveChangesAsync(ct);
    }
}

public sealed class EfBlockedAdvertiserRepository : IBlockedAdvertiserRepository
{
    private readonly ShekarDbContext _db;
    public EfBlockedAdvertiserRepository(ShekarDbContext db) => _db = db;

    public Task<bool> IsBlockedAsync(string phone, DateTimeOffset now, CancellationToken ct = default) =>
        _db.BlockedAdvertisers.AnyAsync(x =>
            x.Phone == phone && x.IsActive && (x.ExpiresAt == null || x.ExpiresAt > now), ct);

    public async Task<IReadOnlyList<string>> ListActivePhonesAsync(DateTimeOffset now, CancellationToken ct = default) =>
        await _db.BlockedAdvertisers
            .Where(x => x.IsActive && (x.ExpiresAt == null || x.ExpiresAt > now))
            .Select(x => x.Phone).Distinct().ToListAsync(ct);

    public async Task<IReadOnlyList<BlockedAdvertiser>> ListAsync(CancellationToken ct = default) =>
        await _db.BlockedAdvertisers.AsNoTracking().OrderByDescending(x => x.BlockedAt).ToListAsync(ct);

    public async Task<BlockedAdvertiser> AddAsync(BlockedAdvertiser entry, CancellationToken ct = default)
    {
        _db.BlockedAdvertisers.Add(entry);
        await _db.SaveChangesAsync(ct);
        return entry;
    }

    public async Task UpdateAsync(BlockedAdvertiser entry, CancellationToken ct = default)
    {
        _db.BlockedAdvertisers.Update(entry);
        await _db.SaveChangesAsync(ct);
    }
}

public sealed class EfPlanLimitRepository : IPlanLimitRepository
{
    private readonly ShekarDbContext _db;
    public EfPlanLimitRepository(ShekarDbContext db) => _db = db;

    public Task<PlanLimit?> GetByPlanAsync(int planId, CancellationToken ct = default) =>
        _db.PlanLimits.FirstOrDefaultAsync(x => x.PlanId == planId, ct);

    public async Task<IReadOnlyList<PlanLimit>> ListAsync(CancellationToken ct = default) =>
        await _db.PlanLimits.AsNoTracking().ToListAsync(ct);
}

public sealed class EfCarListingRepository : ICarListingRepository
{
    private readonly ShekarDbContext _db;
    public EfCarListingRepository(ShekarDbContext db) => _db = db;

    public Task<CarListing?> GetByExternalIdAsync(ListingSource source, string externalId, CancellationToken ct = default) =>
        _db.CarListings.FirstOrDefaultAsync(l => l.Source == source && l.ExternalId == externalId, ct);

    public Task<bool> ExistsActiveByContentHashAsync(string contentHash, CancellationToken ct = default) =>
        _db.CarListings.AnyAsync(l => l.State == ListingState.Active && l.ContentHash == contentHash, ct);

    public async Task<IReadOnlyList<CarListing>> ListActiveAsync(ListingSource source, CancellationToken ct = default) =>
        await _db.CarListings.Where(l => l.Source == source && l.State == ListingState.Active).ToListAsync(ct);

    public async Task<IReadOnlyList<CarListing>> ListUnmatchedAsync(int max, CancellationToken ct = default) =>
        await _db.CarListings
            .Where(l => l.State == ListingState.Active && l.VehicleModelId == null)
            .OrderBy(l => l.Id).Take(max).ToListAsync(ct);

    public async Task<IReadOnlyList<CarListing>> ListMatchedActiveSinceAsync(DateTimeOffset sinceUtc, CancellationToken ct = default) =>
        await _db.CarListings.AsNoTracking()
            .Where(l => l.State == ListingState.Active && l.VehicleModelId != null && l.PublishedAt >= sinceUtc)
            .ToListAsync(ct);

    public async Task<CarListing> AddAsync(CarListing listing, CancellationToken ct = default)
    {
        _db.CarListings.Add(listing);
        await _db.SaveChangesAsync(ct);
        return listing;
    }

    public async Task UpdateAsync(CarListing listing, CancellationToken ct = default)
    {
        _db.CarListings.Update(listing);
        await _db.SaveChangesAsync(ct);
    }
}

public sealed class EfVehicleModelRepository : IVehicleModelRepository
{
    private readonly ShekarDbContext _db;
    public EfVehicleModelRepository(ShekarDbContext db) => _db = db;

    public Task<VehicleModel?> GetAsync(int id, CancellationToken ct = default) =>
        _db.VehicleModels.FirstOrDefaultAsync(m => m.Id == id, ct);

    public async Task<IReadOnlyList<VehicleModel>> ListActiveAsync(CancellationToken ct = default) =>
        await _db.VehicleModels.AsNoTracking().Where(m => m.IsActive).ToListAsync(ct);
}

public sealed class EfMarketSegmentStatRepository : IMarketSegmentStatRepository
{
    private readonly ShekarDbContext _db;
    public EfMarketSegmentStatRepository(ShekarDbContext db) => _db = db;

    public Task<MarketSegmentStat?> GetBySegmentKeyAsync(string segmentKey, CancellationToken ct = default) =>
        _db.MarketSegmentStats.FirstOrDefaultAsync(s => s.SegmentKey == segmentKey, ct);

    public async Task<IReadOnlyList<MarketSegmentStat>> ListAsync(CancellationToken ct = default) =>
        await _db.MarketSegmentStats.AsNoTracking().ToListAsync(ct);

    public async Task UpsertAsync(MarketSegmentStat stat, CancellationToken ct = default)
    {
        if (stat.Id == 0)
        {
            _db.MarketSegmentStats.Add(stat);
        }
        else
        {
            _db.MarketSegmentStats.Update(stat);
        }
        await _db.SaveChangesAsync(ct);
    }
}

public sealed class EfCrawlRunRepository : ICrawlRunRepository
{
    private readonly ShekarDbContext _db;
    public EfCrawlRunRepository(ShekarDbContext db) => _db = db;

    public async Task<CrawlRun> AddAsync(CrawlRun run, CancellationToken ct = default)
    {
        _db.CrawlRuns.Add(run);
        await _db.SaveChangesAsync(ct);
        return run;
    }

    public async Task UpdateAsync(CrawlRun run, CancellationToken ct = default)
    {
        _db.CrawlRuns.Update(run);
        await _db.SaveChangesAsync(ct);
    }
}

public sealed class EfSystemSettingRepository : ISystemSettingRepository
{
    private readonly ShekarDbContext _db;
    public EfSystemSettingRepository(ShekarDbContext db) => _db = db;

    public Task<SystemSetting?> GetAsync(string key, CancellationToken ct = default) =>
        _db.SystemSettings.FirstOrDefaultAsync(x => x.Key == key, ct);

    public async Task<IReadOnlyList<SystemSetting>> ListAsync(CancellationToken ct = default) =>
        await _db.SystemSettings.AsNoTracking().ToListAsync(ct);

    public async Task UpsertAsync(SystemSetting setting, CancellationToken ct = default)
    {
        bool exists = await _db.SystemSettings.AnyAsync(x => x.Key == setting.Key, ct);
        if (exists)
        {
            _db.SystemSettings.Update(setting);
        }
        else
        {
            _db.SystemSettings.Add(setting);
        }
        await _db.SaveChangesAsync(ct);
    }
}
