using Shekar.Application.Abstractions;
using Shekar.Application.Common;
using Shekar.Domain;
using Shekar.Domain.Entities;

namespace Shekar.Application.Services;

/// <summary>ساخت آمار سگمنت‌های بازار (مرحله ۲ پایپ‌لاین، ساعتی).</summary>
public interface IMarketAggregationService
{
    /// <summary>سگمنت‌ها را از آگهی‌های نرمال‌شده‌ی پنجره‌ی اخیر می‌سازد؛ تعداد سگمنت را برمی‌گرداند.</summary>
    Task<int> RunAsync(int windowDays, CancellationToken ct = default);
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

    public async Task<int> RunAsync(int windowDays, CancellationToken ct = default)
    {
        DateTimeOffset now = _clock.UtcNow;
        DateTimeOffset since = now.AddDays(-windowDays);

        IReadOnlyList<CarListing> listings = await _listings.ListMatchedActiveSinceAsync(since, ct);
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

            bool colorInKey = l.PriceToman > DomainConstants.ColorMattersAboveToman
                || (modelById.TryGetValue(vmId, out VehicleModel? vm) && vm.ColorSensitive);
            string? colorKey = colorInKey ? TextNormalizer.Normalize(l.Color) : null;
            if (colorKey is { Length: 0 })
            {
                colorKey = null;
            }

            MileageBucketOf(l, out var bucket);
            string key = $"VM{vmId}|Y{year}|M{(byte)bucket}|B{(byte)l.BodyCondition}|C{colorKey ?? "-"}";

            if (!groups.TryGetValue(key, out SegmentAccumulator? acc))
            {
                acc = new SegmentAccumulator(key, vmId, year, bucket, l.BodyCondition, colorKey);
                groups[key] = acc;
            }
            acc.Prices.Add(l.PriceToman);
        }

        foreach (SegmentAccumulator acc in groups.Values)
        {
            ct.ThrowIfCancellationRequested();
            MarketSegmentStat stat = await _stats.GetBySegmentKeyAsync(acc.Key, ct) ?? new MarketSegmentStat();
            stat.SegmentKey = acc.Key;
            stat.VehicleModelId = acc.VehicleModelId;
            stat.ModelYear = acc.Year;
            stat.MileageBucket = acc.Bucket;
            stat.BodyCondition = acc.BodyCondition;
            stat.ColorKey = acc.ColorKey;
            stat.SampleCount = acc.Prices.Count;
            stat.MedianPriceToman = Statistics.Median(acc.Prices);
            stat.AveragePriceToman = Statistics.Average(acc.Prices);
            stat.P25PriceToman = Statistics.Percentile(acc.Prices, 25);
            stat.P75PriceToman = Statistics.Percentile(acc.Prices, 75);
            stat.WindowDays = windowDays;
            stat.ComputedAt = now;
            await _stats.UpsertAsync(stat, ct);
        }

        return groups.Count;
    }

    private static void MileageBucketOf(CarListing l, out Domain.Enums.MileageBucket bucket) =>
        bucket = DomainConstants.ResolveMileageBucket(l.MileageKm);

    private sealed class SegmentAccumulator
    {
        public SegmentAccumulator(string key, int vmId, int year,
            Domain.Enums.MileageBucket bucket, Domain.Enums.BodyCondition body, string? colorKey)
        {
            Key = key; VehicleModelId = vmId; Year = year; Bucket = bucket; BodyCondition = body; ColorKey = colorKey;
        }

        public string Key { get; }
        public int VehicleModelId { get; }
        public int Year { get; }
        public Domain.Enums.MileageBucket Bucket { get; }
        public Domain.Enums.BodyCondition BodyCondition { get; }
        public string? ColorKey { get; }
        public List<long> Prices { get; } = new();
    }
}
