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

        // سلسله‌مراتبِ سگمنت: L0 دقیق (مدل+سال+کارکرد+بدنه+رنگ)، L1 بدونِ رنگ، L2 بدونِ بدنه،
        // L3 مدل+سال (گردآوریِ همه‌ی کارکردها؛ مبنای شیبِ کارکرد). سطوحِ درشت‌تر فقط وقتی ذخیره
        // می‌شوند که «بیش از یک» سطحِ ریز را ادغام کنند (بدونِ افزونگی).
        var l0 = new Dictionary<string, LevelAccumulator>(StringComparer.Ordinal);
        var l1 = new Dictionary<string, LevelAccumulator>(StringComparer.Ordinal);
        var l2 = new Dictionary<string, LevelAccumulator>(StringComparer.Ordinal);
        var l3 = new Dictionary<string, LevelAccumulator>(StringComparer.Ordinal);

        foreach (CarListing l in listings)
        {
            if (l.VehicleModelId is not { } vmId || l.ModelYear is not { } year)
            {
                continue; // بدون مدل یا سال قابل‌سگمنت نیست
            }

            modelById.TryGetValue(vmId, out VehicleModel? vm);
            string? l0Key = SegmentKey.ForListing(l, vm);
            if (l0Key is null)
            {
                continue;
            }

            MileageBucket bucket = DomainConstants.ResolveMileageBucket(l.MileageKm);
            string? colorKey = SegmentKey.ColorInKey(l.PriceToman, vm) ? TextNormalizer.Normalize(l.Color) : null;
            if (colorKey is { Length: 0 })
            {
                colorKey = null;
            }

            long price = l.PriceToman;
            int mileage = l.MileageKm ?? 0;
            string k1 = SegmentKey.BuildL1(vmId, year, bucket, l.BodyCondition);
            string k2 = SegmentKey.BuildL2(vmId, year, bucket);
            string k3 = SegmentKey.BuildL3(vmId, year);

            Accumulate(l0, l0Key, 0, vmId, year, bucket, l.BodyCondition, colorKey, price, mileage, source: l0Key);
            // L1 رنگ‌ها را در یک بدنه ادغام می‌کند (منبعِ تمایز = کلیدِ دقیق/رنگ).
            Accumulate(l1, k1, 1, vmId, year, bucket, l.BodyCondition, null, price, mileage, source: l0Key);
            // L2 بدنه‌ها را ادغام می‌کند (منبعِ تمایز = کلیدِ L1/بدنه).
            Accumulate(l2, k2, 2, vmId, year, bucket, BodyCondition.Unknown, null, price, mileage, source: k1);
            // L3 کارکردها را گرد می‌آورد (منبعِ تمایز = کلیدِ L2/باکتِ کارکرد).
            Accumulate(l3, k3, 3, vmId, year, bucket, BodyCondition.Unknown, null, price, mileage, source: k2);
        }

        // شیبِ قیمت↔کارکرد در سطحِ (مدل+سال) از استخرِ L3؛ روی همه‌ی سطوحِ همان مدل+سال مُهر می‌شود.
        var slopeByL3 = new Dictionary<string, long>(StringComparer.Ordinal);
        foreach (LevelAccumulator acc in l3.Values)
        {
            slopeByL3[acc.Key] = ComputeMileageSlopePerK(acc, options);
        }

        var currentKeys = new HashSet<string>(StringComparer.Ordinal);
        int reliable = 0;

        long SlopeFor(LevelAccumulator acc) =>
            slopeByL3.TryGetValue(SegmentKey.BuildL3(acc.VehicleModelId, acc.Year), out long sl) ? sl : 0;

        // L0 همیشه ذخیره می‌شود.
        foreach (LevelAccumulator acc in l0.Values)
        {
            ct.ThrowIfCancellationRequested();
            if (await PersistAsync(acc, SlopeFor(acc), options, now, windowDays, ct))
            {
                reliable++;
            }
            currentKeys.Add(acc.Key);
        }

        // سطوحِ درشت فقط اگر واقعاً ادغام کنند (Sources > 1).
        foreach (LevelAccumulator acc in l1.Values.Concat(l2.Values).Concat(l3.Values))
        {
            ct.ThrowIfCancellationRequested();
            if (acc.Sources.Count <= 1)
            {
                continue; // ادغامِ بی‌اثر؛ همتای ریزش کافی است
            }
            if (await PersistAsync(acc, SlopeFor(acc), options, now, windowDays, ct))
            {
                reliable++;
            }
            currentKeys.Add(acc.Key);
        }

        // کهنه‌کردن: هر سگمنتِ قابل‌اتکایی که این پاس تازه نشد دیگر مرجعِ معتبر نیست.
        int staled = 0;
        foreach (MarketSegmentStat old in await _stats.ListAsync(ct))
        {
            if (old.IsReliable && !currentKeys.Contains(old.SegmentKey))
            {
                old.IsReliable = false;
                await _stats.UpsertAsync(old, ct);
                staled++;
            }
        }

        return new AggregationResult(currentKeys.Count, reliable, staled);
    }

    private static void Accumulate(
        Dictionary<string, LevelAccumulator> map, string key, byte level, int vmId, int year,
        MileageBucket bucket, BodyCondition body, string? colorKey, long price, int mileageKm, string source)
    {
        if (!map.TryGetValue(key, out LevelAccumulator? acc))
        {
            acc = new LevelAccumulator(key, level, vmId, year, bucket, body, colorKey);
            map[key] = acc;
        }
        acc.Prices.Add(price);
        acc.Mileages.Add(mileageKm);
        acc.Sources.Add(source);
    }

    /// <summary>
    /// شیبِ قیمت به‌ازای هر ۱۰۰۰ کیلومتر را از نقاطِ (کارکرد، قیمت)ِ یک استخرِ مدل+سال برآورد می‌کند.
    /// پرت‌های قیمت پیش از رگرسیون با حصارِ IQR کنار می‌روند؛ شیب به ≤۰ کلَمپ می‌شود (قیمت با کارکرد
    /// بالا نمی‌رود). اگر نمونه کم یا کارکرد بدونِ پراکندگی بود، <c>0</c> (یعنی بدونِ تعدیل).
    /// </summary>
    private static long ComputeMileageSlopePerK(LevelAccumulator pool, AggregationOptions options)
    {
        if (pool.Prices.Count < options.MinReliableSamples)
        {
            return 0;
        }

        long lo = Statistics.Percentile(pool.Prices, 25);
        long hi = Statistics.Percentile(pool.Prices, 75);
        long iqr = hi - lo;
        long fenceLo = lo - (long)(1.5 * iqr), fenceHi = hi + (long)(1.5 * iqr);

        var points = new List<(double X, double Y)>(pool.Prices.Count);
        for (int i = 0; i < pool.Prices.Count; i++)
        {
            int km = pool.Mileages[i];
            long price = pool.Prices[i];
            if (km > 0 && (iqr <= 0 || (price >= fenceLo && price <= fenceHi)))
            {
                points.Add((km, price));
            }
        }

        if (points.Count < options.MinReliableSamples)
        {
            return 0;
        }

        double slopePerKm = Statistics.Slope(points);        // تومان بر کیلومتر
        long slopePerK = (long)Math.Round(slopePerKm * 1000); // تومان بر ۱۰۰۰ کیلومتر
        return Math.Min(0, slopePerK);                        // قیمت با کارکرد بالا نمی‌رود
    }

    /// <summary>آمارِ مقاومِ یک سگمنت را حساب و upsert می‌کند؛ <c>true</c> اگر قابل‌اتکا شد.</summary>
    private async Task<bool> PersistAsync(
        LevelAccumulator acc, long mileageSlopePerK, AggregationOptions options,
        DateTimeOffset now, int windowDays, CancellationToken ct)
    {
        // پرت‌ها حذف و همه‌ی آماره‌ها با یک‌بار مرتب‌سازی حساب می‌شوند.
        PriceStats s = Statistics.Summarize(acc.Prices);
        bool isReliable = s.SampleCount >= options.MinReliableSamples
            && s.SpreadPercent <= options.MaxReliableSpreadPercent;

        MarketSegmentStat stat = await _stats.GetBySegmentKeyAsync(acc.Key, ct) ?? new MarketSegmentStat();
        stat.SegmentKey = acc.Key;
        stat.Level = acc.Level;
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
        stat.RefMileageKm = MedianMileage(acc.Mileages);
        stat.MileageSlopeTomanPerK = mileageSlopePerK;
        stat.IsReliable = isReliable;
        stat.WindowDays = windowDays;
        stat.ComputedAt = now;
        await _stats.UpsertAsync(stat, ct);
        return isReliable;
    }

    /// <summary>میانه‌ی کارکردِ نمونه‌های دارای کارکردِ معتبر (کیلومتر)؛ ۰ اگر هیچ‌کدام مشخص نبود.</summary>
    private static int MedianMileage(List<int> mileages)
    {
        var valid = mileages.Where(m => m > 0).Select(m => (long)m).ToList();
        return valid.Count == 0 ? 0 : (int)Statistics.Median(valid);
    }

    private sealed record LevelAccumulator(
        string Key, byte Level, int VehicleModelId, int Year, MileageBucket Bucket, BodyCondition BodyCondition, string? ColorKey)
    {
        public List<long> Prices { get; } = new();
        public List<int> Mileages { get; } = new();
        public HashSet<string> Sources { get; } = new(StringComparer.Ordinal);
    }
}
