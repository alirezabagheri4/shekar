using Shekar.Application.Abstractions;
using Shekar.Application.Common;
using Shekar.Domain;
using Shekar.Domain.Entities;
using Shekar.Domain.Enums;

namespace Shekar.Application.Services;

/// <summary>پارامترهای اعتمادسنجیِ سگمنت (از config قابل‌تنظیم).</summary>
public sealed record AggregationOptions
{
    /// <summary>حداقل نمونه برای «قابل‌اتکا» بودنِ سگمنت.</summary>
    public int MinReliableSamples { get; init; } = 5;

    /// <summary>بیشینه‌ی پراکندگیِ نسبی (٪) برای «قابل‌اتکا» بودن؛ بالاتر از آن بازار پراکنده و نامطمئن است.</summary>
    public int MaxReliableSpreadPercent { get; init; } = 60;
}

/// <summary>نتیجه‌ی یک پاس اگریگیشن (برای پایش).</summary>
public sealed record AggregationResult(int Segments, int Reliable, int Staled);

/// <summary>ساخت آمار سگمنت‌های بازار (مرحله ۲ پایپ‌لاین، ساعتی).</summary>
public interface IMarketAggregationService
{
    /// <summary>سگمنت‌ها را از آگهی‌های نرمال‌شده‌ی پنجره‌ی اخیر می‌سازد؛ تعداد سگمنت را برمی‌گرداند.</summary>
    Task<int> RunAsync(int windowDays, CancellationToken ct = default);

    /// <summary>نسخه‌ی کامل با اعتمادسنجی و کهنه‌کردنِ سگمنت‌های تازه‌نشده.</summary>
    Task<AggregationResult> RunAsync(int windowDays, AggregationOptions options, CancellationToken ct = default);
}

/// <summary>
/// سگمنت = مدل + سال + باکتِ‌کارکرد + وضعیت‌بدنه [+ رنگ اگر قیمت &gt; ۴ میلیارد یا مدل حساس به رنگ].
/// برای هر سگمنت: میانه/میانگین/چارک‌ها + تعداد نمونه. upsert در MarketSegmentStats.
/// </summary>
public sealed class MarketAggregationService : IMarketAggregationService
{
    private readonly ICarListingRepository _listings;
    private readonly IVehicleModelRepository _models;
    private readonly IMarketSegmentStatRepository _stats;
    private readonly IClock _clock;

    public MarketAggregationService(
        ICarListingRepository listings, IVehicleModelRepository models,
        IMarketSegmentStatRepository stats, IClock clock)
    {
        _listings = listings;
        _models = models;
        _stats = stats;
        _clock = clock;
    }

    public async Task<int> RunAsync(int windowDays, CancellationToken ct = default) =>
        (await RunAsync(windowDays, new AggregationOptions(), ct)).Segments;

    public async Task<AggregationResult> RunAsync(int windowDays, AggregationOptions options, CancellationToken ct = default)
    {
        DateTimeOffset now = _clock.UtcNow;
        DateTimeOffset since = now.AddDays(-windowDays);

        // پنجره‌ی اگریگیشن شاملِ آگهی‌های «دیده‌شده» (فعال + به‌تازگی‌حذف‌شده) است تا تاریخچه
        // انباشته و نمونه‌ی سگمنت به حدنصاب برسد.
        IReadOnlyList<CarListing> listings = await _listings.ListMatchedSeenSinceAsync(since, ct);
        IReadOnlyList<VehicleModel> models = await _models.ListActiveAsync(ct);
        Dictionary<int, VehicleModel> modelById = models.ToDictionary(m => m.Id);

        // گروه‌بندی بر اساس کلید سگمنت.
        var groups = new Dictionary<string, SegmentAccumulator>();

        foreach (CarListing l in listings)
        {
            if (l.VehicleModelId is not { } vmId || l.ModelYear is not { } year)
            {
                continue; // بدون مدل یا سال قابل‌سگمنت نیست
            }

            modelById.TryGetValue(vmId, out VehicleModel? vm);
            string? key = SegmentKey.ForListing(l, vm);
            if (key is null)
            {
                continue;
            }

            MileageBucket bucket = DomainConstants.ResolveMileageBucket(l.MileageKm);
            string? colorKey = SegmentKey.ColorInKey(l.PriceToman, vm) ? TextNormalizer.Normalize(l.Color) : null;
            if (colorKey is { Length: 0 })
            {
                colorKey = null;
            }

            if (!groups.TryGetValue(key, out SegmentAccumulator? acc))
            {
                acc = new SegmentAccumulator(key, vmId, year, bucket, l.BodyCondition, colorKey);
                groups[key] = acc;
            }
            acc.Prices.Add(l.PriceToman);
        }

        int reliable = 0;
        foreach (SegmentAccumulator acc in groups.Values)
        {
            ct.ThrowIfCancellationRequested();
            // آمارِ مقاوم: پرت‌ها حذف و همه‌ی آماره‌ها با یک‌بار مرتب‌سازی حساب می‌شوند.
            // SampleCount = تعدادِ نمونه‌ی به‌کاررفته پس از تریم.
            PriceStats s = Statistics.Summarize(acc.Prices);
            bool isReliable = s.SampleCount >= options.MinReliableSamples
                && s.SpreadPercent <= options.MaxReliableSpreadPercent;
            if (isReliable)
            {
                reliable++;
            }

            MarketSegmentStat stat = await _stats.GetBySegmentKeyAsync(acc.Key, ct) ?? new MarketSegmentStat();
            stat.SegmentKey = acc.Key;
            stat.VehicleModelId = acc.VehicleModelId;
            stat.ModelYear = acc.Year;
            stat.MileageBucket = acc.Bucket;
            stat.BodyCondition = acc.BodyCondition;
            stat.ColorKey = acc.ColorKey;
            stat.SampleCount = s.SampleCount;
            stat.RawSampleCount = acc.Prices.Count;
            stat.MedianPriceToman = s.Median;
            stat.AveragePriceToman = s.Average;
            stat.MinPriceToman = s.Min;
            stat.P25PriceToman = s.P25;
            stat.P75PriceToman = s.P75;
            stat.SpreadPercent = s.SpreadPercent;
            stat.IsReliable = isReliable;
            stat.WindowDays = windowDays;
            stat.ComputedAt = now;
            await _stats.UpsertAsync(stat, ct);
        }

        // کهنه‌کردن: سگمنت‌هایی که این پاس تازه نشدند دیگر مرجعِ قابل‌اتکا نیستند
        // (مدل/سال از پنجره خارج شده). مدیانِ قدیمی را نگه می‌داریم ولی IsReliable=false.
        int staled = 0;
        foreach (MarketSegmentStat old in await _stats.ListAsync(ct))
        {
            if (old.IsReliable && !groups.ContainsKey(old.SegmentKey))
            {
                old.IsReliable = false;
                await _stats.UpsertAsync(old, ct);
                staled++;
            }
        }

        return new AggregationResult(groups.Count, reliable, staled);
    }

    private sealed record SegmentAccumulator(
        string Key, int VehicleModelId, int Year, MileageBucket Bucket, BodyCondition BodyCondition, string? ColorKey)
    {
        public List<long> Prices { get; } = new();
    }
}
