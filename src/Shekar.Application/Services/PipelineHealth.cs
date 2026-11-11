using Shekar.Application.Abstractions;
using Shekar.Domain.Entities;
using Shekar.Domain.Enums;

namespace Shekar.Application.Services;

/// <summary>شدتِ یک هشدارِ سلامت.</summary>
public enum HealthLevel
{
    /// <summary>سالم.</summary>
    Ok = 0,

    /// <summary>اطلاع‌رسانی؛ لزوماً مشکل نیست.</summary>
    Info = 1,

    /// <summary>هشدار؛ باید سر زد.</summary>
    Warning = 2,

    /// <summary>بحرانی؛ خط لوله احتمالاً خوابیده یا بلاک شده.</summary>
    Alarm = 3
}

/// <summary>یک یافته‌ی پایش.</summary>
public sealed record HealthAlert(HealthLevel Level, string Message);

/// <summary>آستانه‌های پایش سلامت خط لوله (از config قابل‌تنظیم).</summary>
public sealed record PipelineHealthOptions
{
    /// <summary>اگر از آخرین کراولِ موفق بیش از این ساعت گذشته باشد، «کراول متوقف» است.</summary>
    public int StaleCrawlHours { get; init; } = 6;

    /// <summary>کفِ نرخ تطبیق (٪)؛ پایین‌تر از آن یعنی نرمال‌سازی ضعیف است.</summary>
    public int MinMatchRatePercent { get; init; } = 50;

    /// <summary>
    /// کفِ نسبتِ «سگمنت‌پذیر به نرمال‌شده» (٪). اگر مدل داریم ولی سالِ کمی داریم، یعنی غنی‌سازیِ
    /// جزئیات (صفحه‌ی /v) خوب کار نمی‌کند (چلنجِ ضدبات؟) و ورودیِ اسکورینگ تهی می‌ماند.
    /// </summary>
    public int MinSegmentableOfMatchedPercent { get; init; } = 60;

    /// <summary>حداقل تعداد آگهی فعال برای اینکه نرخ‌ها معنا پیدا کنند (زیر آن سکوت).</summary>
    public int MinListingsForRate { get; init; } = 20;

    /// <summary>تعداد پاسِ اخیر کراول برای نمایش در گزارش.</summary>
    public int RecentRuns { get; init; } = 10;
}

/// <summary>گزارشِ لحظه‌ایِ سلامت خط لوله (کراول → نرمال‌سازی/اگریگیشن → اسکورینگ).</summary>
public sealed record PipelineHealthReport(
    DateTimeOffset GeneratedAt,
    HealthLevel Overall,
    CrawlRun? LastCrawl,
    bool CrawlStale,
    int ActiveListings,
    int MatchedListings,
    int UnmatchedListings,
    int MatchRatePercent,
    int SegmentableListings,
    int SegmentableRatePercent,
    int Segments,
    int ReliableSegments,
    int RegisteredModels,
    int ActiveHunts,
    int QualifiedRatePercent,
    IReadOnlyList<CrawlRun> RecentCrawls,
    IReadOnlyList<HealthAlert> Alerts);

/// <summary>
/// سلامتِ خط لوله را از وضعیتِ فعلیِ مخازن می‌سنجد و هشدارهای عملیاتی می‌سازد
/// (صفر آگهی، کراولِ متوقف، نرخ تطبیق پایین، نبودِ سگمنت). بدونِ وابستگی به کاربر.
/// </summary>
public interface IPipelineHealthService
{
    Task<PipelineHealthReport> GetAsync(PipelineHealthOptions options, CancellationToken ct = default);
}

public sealed class PipelineHealthService : IPipelineHealthService
{
    private readonly ICrawlRunRepository _runs;
    private readonly ICarListingRepository _listings;
    private readonly IMarketSegmentStatRepository _stats;
    private readonly IVehicleModelRepository _models;
    private readonly IHuntRepository _hunts;
    private readonly IClock _clock;

    public PipelineHealthService(
        ICrawlRunRepository runs, ICarListingRepository listings, IMarketSegmentStatRepository stats,
        IVehicleModelRepository models, IHuntRepository hunts, IClock clock)
    {
        _runs = runs;
        _listings = listings;
        _stats = stats;
        _models = models;
        _hunts = hunts;
        _clock = clock;
    }

    public async Task<PipelineHealthReport> GetAsync(PipelineHealthOptions options, CancellationToken ct = default)
    {
        DateTimeOffset now = _clock.UtcNow;

        IReadOnlyList<CrawlRun> recent = await _runs.ListRecentAsync(Math.Max(1, options.RecentRuns), ct);
        CrawlRun? lastCrawl = recent.Count > 0 ? recent[0] : null;

        // شمارشِ وضعیت با COUNT (بدونِ بارگذاریِ ردیف‌ها).
        int active = await _listings.CountActiveAsync(ct);
        int matched = await _listings.CountMatchedActiveAsync(ct);       // دارای مدل
        int segmentable = await _listings.CountSegmentableActiveAsync(ct); // دارای مدل و سال (ورودیِ واقعی)
        int unmatched = active - matched;
        int matchRate = active > 0 ? (int)Math.Round(matched * 100.0 / active) : 0;
        int segmentableRate = active > 0 ? (int)Math.Round(segmentable * 100.0 / active) : 0;

        IReadOnlyList<MarketSegmentStat> allStats = await _stats.ListAsync(ct);
        int segments = allStats.Count;
        int reliableSegments = allStats.Count(s => s.IsReliable);
        int models = (await _models.ListActiveAsync(ct)).Count;
        int activeHunts = (await _hunts.ListPipelineAsync(ct)).Count(h => h.State == HuntState.Active);
        // نرخِ واجدشرایط را نسبت به «سگمنت‌پذیر» می‌سنجیم چون ورودیِ واقعیِ اسکورینگ همان است.
        int qualifiedRate = segmentable > 0 ? (int)Math.Round(activeHunts * 100.0 / segmentable) : 0;

        // آخرین کراولِ موفق برای تشخیصِ «متوقف».
        CrawlRun? lastSuccess = recent.FirstOrDefault(r => r.Status == CrawlStatus.Succeeded);
        DateTimeOffset? lastSuccessAt = lastSuccess?.FinishedAt ?? lastSuccess?.StartedAt;
        bool crawlStale = lastSuccessAt is null || (now - lastSuccessAt.Value).TotalHours > options.StaleCrawlHours;

        var alerts = new List<HealthAlert>();

        if (lastCrawl is null)
        {
            alerts.Add(new HealthAlert(HealthLevel.Alarm, "هیچ پاسِ کراولی ثبت نشده — خط لوله هنوز اجرا نشده است."));
        }
        else
        {
            if (crawlStale)
            {
                alerts.Add(new HealthAlert(HealthLevel.Alarm,
                    $"کراول متوقف است — آخرین پاسِ موفق بیش از {options.StaleCrawlHours} ساعت پیش بوده."));
            }

            if (lastCrawl.Status == CrawlStatus.Failed)
            {
                alerts.Add(new HealthAlert(HealthLevel.Alarm,
                    $"آخرین پاسِ کراول با خطا تمام شد: {lastCrawl.Error ?? "نامشخص"}"));
            }

            if (lastCrawl.Status == CrawlStatus.Succeeded && lastCrawl.FetchedCount == 0)
            {
                alerts.Add(new HealthAlert(HealthLevel.Alarm,
                    "آخرین کراول صفر آگهی گرفت — احتمالِ تغییرِ ساختارِ سایت یا بلاک‌شدن."));
            }
        }

        if (active >= options.MinListingsForRate && matchRate < options.MinMatchRatePercent)
        {
            alerts.Add(new HealthAlert(HealthLevel.Warning,
                $"نرخ تطبیق پایین است ({matchRate}٪) — نرمال‌سازی ضعیف؛ آگهی‌های بی‌تطبیق را بررسی کنید."));
        }

        // توقفِ غنی‌سازیِ سال: مدل داریم ولی سال نه → ورودیِ اسکورینگ تهی می‌ماند (علامتِ چلنجِ ضدبات).
        if (matched >= options.MinListingsForRate
            && segmentable * 100 < matched * options.MinSegmentableOfMatchedPercent)
        {
            int segOfMatched = matched > 0 ? (int)Math.Round(segmentable * 100.0 / matched) : 0;
            alerts.Add(new HealthAlert(HealthLevel.Warning,
                $"غنی‌سازیِ جزئیات لنگ می‌زند — فقط {segOfMatched}٪ از آگهی‌های نرمال‌شده سال دارند؛ " +
                "فچِ صفحه‌ی جزئیات (چلنجِ ضدبات؟) را بررسی کنید."));
        }

        if (matched > 0 && segments == 0)
        {
            alerts.Add(new HealthAlert(HealthLevel.Warning,
                "آگهیِ نرمال‌شده هست ولی هیچ سگمنتِ بازاری ساخته نشده — اگریگیشن را بررسی کنید."));
        }

        if (segments > 0 && reliableSegments == 0)
        {
            alerts.Add(new HealthAlert(HealthLevel.Warning,
                "هیچ سگمنتِ قابل‌اتکایی نیست — نمونه‌ها کم یا پراکنده‌اند؛ مرجعِ قیمت برای اسکورینگ ضعیف است."));
        }

        if (reliableSegments > 0 && activeHunts == 0)
        {
            alerts.Add(new HealthAlert(HealthLevel.Info,
                "سگمنتِ قابل‌اتکا هست ولی هیچ شکارِ فعالی نیست — یا بازار منصفانه قیمت خورده یا آستانه‌ها سخت‌اند."));
        }

        HealthLevel overall = alerts.Count == 0 ? HealthLevel.Ok : alerts.Max(a => a.Level);
        if (alerts.Count == 0)
        {
            alerts.Add(new HealthAlert(HealthLevel.Ok, "خط لوله سالم است."));
        }

        return new PipelineHealthReport(
            now, overall, lastCrawl, crawlStale, active, matched, unmatched, matchRate,
            segmentable, segmentableRate, segments, reliableSegments, models, activeHunts, qualifiedRate, recent, alerts);
    }
}
