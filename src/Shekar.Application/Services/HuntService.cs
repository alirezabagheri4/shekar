using Shekar.Application.Abstractions;
using Shekar.Application.Common;
using Shekar.Application.Contracts;
using Shekar.Domain;
using Shekar.Domain.Entities;
using Shekar.Domain.Enums;

namespace Shekar.Application.Services;

/// <summary>لیست و جزئیات شکارها. قفل (R4)، حذف‌شده (R7)، فرصت طلایی (R8)، سود (R9)، تب/مرتب‌سازی (R11).</summary>
public sealed class HuntService : IHuntService
{
    private static readonly IReadOnlyDictionary<PriceCategory, string> CategoryImages =
        new Dictionary<PriceCategory, string>
        {
            [PriceCategory.Under1B] = "cat-01-under-1b.webp",
            [PriceCategory.From1To1_5B] = "cat-02-1-1.5b.webp",
            [PriceCategory.From1_5To2B] = "cat-03-1.5-2b.webp",
            [PriceCategory.From2To3B] = "cat-04-2-3b.webp",
            [PriceCategory.From3To4B] = "cat-05-3-4b.webp",
            [PriceCategory.From4To8B] = "cat-06-4-8b.webp",
            [PriceCategory.From8To16B] = "cat-07-8-16b.webp",
            [PriceCategory.Above16B] = "cat-08-above-16b.webp"
        };

    private readonly IHuntRepository _hunts;
    private readonly IBellAlertRepository _bells;
    private readonly ISubscriptionService _subscriptions;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public HuntService(
        IHuntRepository hunts,
        IBellAlertRepository bells,
        ISubscriptionService subscriptions,
        ICurrentUser currentUser,
        IClock clock)
    {
        _hunts = hunts;
        _bells = bells;
        _subscriptions = subscriptions;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<HuntListDto> GetListAsync(PriceCategory? category, HuntDay day, HuntSortOrder sort, CancellationToken ct = default)
    {
        // R4 — لیست شکار فقط برای کاربر لاگین‌شده؛ مهمان باید وارد شود.
        int userId = _currentUser.RequireUserId();
        SubscriptionStatusDto status = await _subscriptions.GetStatusForUserAsync(userId, ct);

        int cityId = await ResolveCityAsync(userId, ct);
        var query = new HuntQuery(cityId, category, day, sort);
        IReadOnlyList<Hunt> hunts = await _hunts.QueryAsync(query, ct);

        IReadOnlyList<Hunt> sorted = ApplySort(hunts, sort);
        List<HuntCardDto> cards = sorted.Select(MapCard).ToList();

        // R11 — «شکار فعال» یعنی آگهی‌های حذف‌نشده.
        int activeCount = cards.Count(c => !c.IsRemoved);
        bool isLocked = !status.IsSubscriber;

        return new HuntListDto
        {
            Category = category,
            Day = day,
            Sort = sort,
            IsLocked = isLocked,
            ActiveCount = activeCount,
            Cards = cards,
            LockedBanner = isLocked ? BuildBanner(cards) : null
        };
    }

    public async Task<HuntDetailDto> GetDetailAsync(int id, CancellationToken ct = default)
    {
        // R4 — مهمان → ۴۰۱.
        int userId = _currentUser.RequireUserId();
        SubscriptionStatusDto status = await _subscriptions.GetStatusForUserAsync(userId, ct);

        // R4 — کاربر بدون اشتراک جزئیات نمی‌بیند → ۴۰۳.
        if (!status.IsSubscriber)
        {
            throw new ForbiddenException(ErrorCodes.HuntLocked, "این شکار هنوز قفله.");
        }

        Hunt? hunt = await _hunts.GetAsync(id, ct);

        // R7 — آگهی حذف‌شده جزئیات ندارد → ۴۰۴.
        if (hunt is null || hunt.State == HuntState.Removed)
        {
            throw new NotFoundException("این آگهی حذف شده؛ جزئیاتش دیگه در دسترس نیست.");
        }

        return MapDetail(hunt);
    }

    public async Task<IReadOnlyList<CategorySummaryDto>> GetCategoriesAsync(CancellationToken ct = default)
    {
        int cityId = _currentUser.IsAuthenticated
            ? await ResolveCityAsync(_currentUser.UserId!.Value, ct)
            : 1;

        IReadOnlyList<Hunt> today = await _hunts.ListTodayAsync(cityId, ct);

        // شمارش دسته‌ها همه‌ی شکارهای امروز (شاملِ حذف‌شده‌ها) را در بر می‌گیرد؛ مطابق سایدبار نمونه.
        Dictionary<PriceCategory, int> countByCategory = today
            .GroupBy(h => DomainConstants.ResolveCategory(h.PriceToman))
            .ToDictionary(g => g.Key, g => g.Count());

        HashSet<PriceCategory> onBells = new();
        if (_currentUser.IsAuthenticated)
        {
            DateTimeOffset now = _clock.UtcNow;
            IReadOnlyList<BellAlert> bells = await _bells.ListForUserAsync(_currentUser.UserId!.Value, ct);
            onBells = bells
                .Where(b => b.ExpiresAt is not null && b.ExpiresAt > now)
                .Select(b => b.Category)
                .ToHashSet();
        }

        return DomainConstants.PriceCategoryBands.Select(band => new CategorySummaryDto
        {
            Category = band.Category,
            Label = band.Label,
            TodayCount = countByCategory.GetValueOrDefault(band.Category),
            ImageFileName = CategoryImages.GetValueOrDefault(band.Category),
            BellOn = onBells.Contains(band.Category)
        }).ToList();
    }

    private async Task<int> ResolveCityAsync(int userId, CancellationToken ct)
    {
        // در MVP فقط تهران فعال است؛ شهر کاربر از وضعیت اشتراک/پیش‌فرض گرفته می‌شود.
        _ = userId;
        _ = ct;
        return await Task.FromResult(1);
    }

    private static IReadOnlyList<Hunt> ApplySort(IReadOnlyList<Hunt> hunts, HuntSortOrder sort) => sort switch
    {
        // R11 — مرتب‌سازی. حذف‌شده‌ها همیشه ته لیست.
        HuntSortOrder.MostBelowMarket => hunts
            .OrderBy(h => h.State == HuntState.Removed)
            .ThenByDescending(h => h.BelowMarketPercent)
            .ToList(),
        HuntSortOrder.Newest => hunts
            .OrderBy(h => h.State == HuntState.Removed)
            .ThenByDescending(h => h.PublishedAt)
            .ToList(),
        _ => hunts
            .OrderBy(h => h.State == HuntState.Removed)
            .ThenByDescending(h => h.Score)
            .ToList()
    };

    private static LockedBannerDto BuildBanner(IReadOnlyList<HuntCardDto> cards)
    {
        List<HuntCardDto> active = cards.Where(c => !c.IsRemoved).ToList();
        return new LockedBannerDto
        {
            TodayHuntCount = active.Count,
            AverageBelowMarketPercent = active.Count == 0
                ? 0
                : (int)Math.Round(active.Average(c => c.BelowMarketPercent)),
            MaxProfitToman = active.Count == 0 ? 0 : active.Max(c => c.EstimatedProfitToman),
            GoldenCount = active.Count(c => c.IsGolden)
        };
    }

    private HuntCardDto MapCard(Hunt h) => new()
    {
        Id = h.Id,
        Title = h.Title,
        ModelYear = h.ModelYear,
        MileageKm = h.MileageKm,
        Location = h.Location,
        PriceToman = h.PriceToman,
        MarketMedianToman = h.MarketMedianToman,
        BelowMarketPercent = h.BelowMarketPercent,
        Score = h.Score,
        IsGolden = HuntCalculations.IsGolden(h.Score),
        IsNew = h.IsNew,
        Liquidity = h.Liquidity,
        EstimatedProfitToman = HuntCalculations.EstimatedNetProfit(h.MarketMedianToman, h.PriceToman),
        PublishedAt = h.PublishedAt,
        ImageFileName = h.ImageFileName,
        Category = DomainConstants.ResolveCategory(h.PriceToman),
        IsRemoved = h.State == HuntState.Removed
    };

    private HuntDetailDto MapDetail(Hunt h)
    {
        HuntAnalysis a = h.Analysis ?? new HuntAnalysis();
        return new HuntDetailDto
        {
            Card = MapCard(h),
            ComparableCount = a.ComparableCount,
            Confidence = a.Confidence,
            FairRangeMinToman = a.FairRangeMinToman,
            FairRangeMaxToman = a.FairRangeMaxToman,
            Summary = a.Summary,
            Factors = a.Factors.Select(f => new AnalysisFactorDto(f.Name, f.Rating, f.Detail, f.Score)).ToList(),
            Risks = a.Risks,
            Color = h.Color,
            BrandModel = h.BrandModel,
            GearboxType = h.GearboxType,
            FuelType = h.FuelType,
            InsuranceMonths = h.InsuranceMonths,
            EngineCondition = h.EngineCondition,
            ChassisCondition = h.ChassisCondition,
            BodyConditionText = h.BodyConditionText,
            GearboxCondition = h.GearboxCondition,
            BasePriceToman = h.BasePriceToman,
            SellerDescription = h.SellerDescription,
            SellerName = h.SellerName,
            SourceUrl = h.SourceUrl,
            ImageCount = h.ImageCount,
            AiSummary = h.AiSummary,
            ResellDaysMin = h.ResellDaysMin,
            ResellDaysMax = h.ResellDaysMax,
            TransactionCostToman = HuntCalculations.TransactionCost(h.PriceToman)
        };
    }
}
