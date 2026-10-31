using Shekar.Domain.Entities;
using Shekar.Domain.Enums;

namespace Shekar.Application.Abstractions;

/// <summary>فیلترهای واکشی لیست شکارها (R11).</summary>
public sealed record HuntQuery(
    int CityId,
    PriceCategory? Category,
    HuntDay Day,
    HuntSortOrder Sort);

public interface ICityRepository
{
    Task<City?> GetAsync(int id, CancellationToken ct = default);
    Task<City?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<IReadOnlyList<City>> ListAsync(CancellationToken ct = default);
}

public interface IPlanRepository
{
    Task<IReadOnlyList<Plan>> ListAsync(CancellationToken ct = default);
    Task<Plan?> GetAsync(PlanType type, CancellationToken ct = default);
}

public interface IUserRepository
{
    Task<User?> GetAsync(int id, CancellationToken ct = default);
    Task<User?> GetByMobileAsync(string mobile, CancellationToken ct = default);
    Task<User> AddAsync(User user, CancellationToken ct = default);
    Task UpdateAsync(User user, CancellationToken ct = default);
}

public interface ISubscriptionRepository
{
    Task<Subscription?> GetActiveForUserAsync(int userId, DateTimeOffset now, CancellationToken ct = default);

    /// <summary>R1 — تعداد اشتراک‌های فعال (Trial + Active و منقضی‌نشده) یک شهر.</summary>
    Task<int> CountActiveInCityAsync(int cityId, DateTimeOffset now, CancellationToken ct = default);

    Task<Subscription> AddAsync(Subscription subscription, CancellationToken ct = default);
}

public interface IHuntRepository
{
    Task<IReadOnlyList<Hunt>> QueryAsync(HuntQuery query, CancellationToken ct = default);

    /// <summary>همه‌ی شکارهای امروزِ یک شهر (برای بنر قفل و شمارش دسته‌ها).</summary>
    Task<IReadOnlyList<Hunt>> ListTodayAsync(int cityId, CancellationToken ct = default);

    Task<Hunt?> GetAsync(int id, CancellationToken ct = default);
}

public interface IAppraisalRepository
{
    /// <summary>R5 — تعداد کارشناسی‌های کاربر در بازه‌ی [from, to).</summary>
    Task<int> CountForUserInRangeAsync(int userId, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken ct = default);

    Task<IReadOnlyList<Appraisal>> ListForUserAsync(int userId, CancellationToken ct = default);

    Task<Appraisal> AddAsync(Appraisal appraisal, CancellationToken ct = default);
}

public interface IBellAlertRepository
{
    Task<IReadOnlyList<BellAlert>> ListForUserAsync(int userId, CancellationToken ct = default);
    Task<BellAlert?> GetAsync(int userId, PriceCategory category, CancellationToken ct = default);
    Task UpsertAsync(BellAlert alert, CancellationToken ct = default);
}

public interface IWaitlistRepository
{
    Task<WaitlistEntry?> GetAsync(int cityId, string mobile, CancellationToken ct = default);
    Task<int> CountForCityAsync(int cityId, CancellationToken ct = default);
    Task<WaitlistEntry> AddAsync(WaitlistEntry entry, CancellationToken ct = default);
}

public interface IOtpRepository
{
    Task<OtpCode?> GetLatestAsync(string mobile, CancellationToken ct = default);
    Task AddAsync(OtpCode code, CancellationToken ct = default);
    Task UpdateAsync(OtpCode code, CancellationToken ct = default);
}

public interface IAdvertiserRepository
{
    Task<Advertiser?> GetByPhoneAsync(string phone, CancellationToken ct = default);
    Task<IReadOnlyList<Advertiser>> ListAsync(CancellationToken ct = default);
    Task UpsertAsync(Advertiser advertiser, CancellationToken ct = default);
}

public interface IBlockedAdvertiserRepository
{
    /// <summary>آیا این شماره هم‌اکنون بلاک فعال دارد.</summary>
    Task<bool> IsBlockedAsync(string phone, DateTimeOffset now, CancellationToken ct = default);

    /// <summary>فهرست شماره‌های بلاک فعال (برای حذف از پیشنهادهای روزانه).</summary>
    Task<IReadOnlyList<string>> ListActivePhonesAsync(DateTimeOffset now, CancellationToken ct = default);

    Task<IReadOnlyList<BlockedAdvertiser>> ListAsync(CancellationToken ct = default);
    Task<BlockedAdvertiser> AddAsync(BlockedAdvertiser entry, CancellationToken ct = default);
    Task UpdateAsync(BlockedAdvertiser entry, CancellationToken ct = default);
}

public interface IPlanLimitRepository
{
    Task<PlanLimit?> GetByPlanAsync(int planId, CancellationToken ct = default);
    Task<IReadOnlyList<PlanLimit>> ListAsync(CancellationToken ct = default);
}

public interface ISystemSettingRepository
{
    Task<SystemSetting?> GetAsync(string key, CancellationToken ct = default);
    Task<IReadOnlyList<SystemSetting>> ListAsync(CancellationToken ct = default);
    Task UpsertAsync(SystemSetting setting, CancellationToken ct = default);
}

public interface ICarListingRepository
{
    /// <summary>آگهی موجود با همین (منبع، شناسه‌ی منبع)؛ برای جلوگیری از تکرار.</summary>
    Task<CarListing?> GetByExternalIdAsync(ListingSource source, string externalId, CancellationToken ct = default);

    /// <summary>آیا آگهی فعالِ دیگری با همین اثرانگشت محتوا وجود دارد (تکرار نزدیک).</summary>
    Task<bool> ExistsActiveByContentHashAsync(string contentHash, CancellationToken ct = default);

    /// <summary>آگهی‌های فعالِ یک منبع (برای تشخیص حذف‌شده‌ها).</summary>
    Task<IReadOnlyList<CarListing>> ListActiveAsync(ListingSource source, CancellationToken ct = default);

    /// <summary>آگهی‌های فعالِ نرمال‌نشده (VehicleModelId خالی) برای نرمال‌سازی.</summary>
    Task<IReadOnlyList<CarListing>> ListUnmatchedAsync(int max, CancellationToken ct = default);

    /// <summary>آگهی‌های فعالِ نرمال‌شده‌ی منتشرشده از زمان مشخص (ورودیِ اگریگیشن).</summary>
    Task<IReadOnlyList<CarListing>> ListMatchedActiveSinceAsync(DateTimeOffset sinceUtc, CancellationToken ct = default);

    Task<CarListing> AddAsync(CarListing listing, CancellationToken ct = default);
    Task UpdateAsync(CarListing listing, CancellationToken ct = default);
}

public interface IVehicleModelRepository
{
    Task<VehicleModel?> GetAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<VehicleModel>> ListActiveAsync(CancellationToken ct = default);
}

public interface IMarketSegmentStatRepository
{
    Task<MarketSegmentStat?> GetBySegmentKeyAsync(string segmentKey, CancellationToken ct = default);
    Task<IReadOnlyList<MarketSegmentStat>> ListAsync(CancellationToken ct = default);
    Task UpsertAsync(MarketSegmentStat stat, CancellationToken ct = default);
}

public interface ICrawlRunRepository
{
    Task<CrawlRun> AddAsync(CrawlRun run, CancellationToken ct = default);
    Task UpdateAsync(CrawlRun run, CancellationToken ct = default);
}
