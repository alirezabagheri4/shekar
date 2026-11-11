using Shekar.Application.Abstractions;
using Shekar.Application.Common;
using Shekar.Domain.Entities;
using Shekar.Domain.Enums;

namespace Shekar.Application.Services;

/// <summary>نتیجه‌ی یک پاس اسکورینگ.</summary>
public sealed record ScoringRunResult(
    int Evaluated, int Qualified, int Created, int Updated, int Removed, int Suspicious, int Duplicates);

/// <summary>اسکورینگ آگهی‌ها و جای‌گذاری در تاپ‌ها (مرحله ۳ پایپ‌لاین).</summary>
public interface IListingScoringService
{
    Task<ScoringRunResult> RunAsync(ScoringOptions options, int windowHours, CancellationToken ct = default);
}

/// <summary>
/// برای آگهی‌های نرمال‌شده‌ی اخیر، امتیاز را نسبت به میانه‌ی سگمنت محاسبه و واجدین شرایط را
/// به‌صورت Hunt (تاپ) درج/به‌روزرسانی می‌کند. شکارهای پایپ‌لاینی که دیگر واجد شرایط نیستند
/// (یا آگهی‌شان حذف شده) به Removed تغییر می‌کنند.
/// </summary>
public sealed class ListingScoringService : IListingScoringService
{
    private readonly ICarListingRepository _listings;
    private readonly IVehicleModelRepository _models;
    private readonly IMarketSegmentStatRepository _stats;
    private readonly IHuntRepository _hunts;
    private readonly IHuntScorer _scorer;
    private readonly IClock _clock;

    public ListingScoringService(
        ICarListingRepository listings, IVehicleModelRepository models, IMarketSegmentStatRepository stats,
        IHuntRepository hunts, IHuntScorer scorer, IClock clock)
    {
        _listings = listings;
        _models = models;
        _stats = stats;
        _hunts = hunts;
        _scorer = scorer;
        _clock = clock;
    }

    public async Task<ScoringRunResult> RunAsync(ScoringOptions options, int windowHours, CancellationToken ct = default)
    {
        DateTimeOffset now = _clock.UtcNow;
        DateTimeOffset since = now.AddHours(-windowHours);

        IReadOnlyList<CarListing> listings = await _listings.ListMatchedActiveSinceAsync(since, ct);
        Dictionary<int, VehicleModel> models = (await _models.ListActiveAsync(ct)).ToDictionary(m => m.Id);

        IReadOnlyList<Hunt> pipelineHunts = await _hunts.ListPipelineAsync(ct);
        Dictionary<long, Hunt> huntByListing = pipelineHunts
            .Where(h => h.SourceListingId is not null)
            .ToDictionary(h => h.SourceListingId!.Value);

        int evaluated = 0, qualified = 0, created = 0, updated = 0, removed = 0, suspicious = 0, duplicates = 0;
        var qualifyingListingIds = new HashSet<long>();
        // اثرانگشتِ آگهی‌های سطح‌شده در این پاس؛ برای حذفِ نزدیک‌تکرارهای دلال (یک خودرو، چند آگهی).
        var seenFingerprints = new HashSet<string>(StringComparer.Ordinal);

        foreach (CarListing listing in listings)
        {
            ct.ThrowIfCancellationRequested();
            if (listing.VehicleModelId is not { } vmId || !models.TryGetValue(vmId, out VehicleModel? model))
            {
                continue;
            }

            string? key = SegmentKey.ForListing(listing, model);
            if (key is null)
            {
                continue;
            }

            MarketSegmentStat? segment = await _stats.GetBySegmentKeyAsync(key, ct);
            if (segment is null)
            {
                continue; // هنوز آمار سگمنت ساخته نشده
            }

            evaluated++;
            ScoreResult r = _scorer.Score(listing, segment, model, options);

            listing.IsScored = true;
            await _listings.UpdateAsync(listing, ct);

            if (r.IsSuspicious)
            {
                suspicious++; // زیربازارِ غیرمنطقی؛ سطح نمی‌شود (محافظِ کلاه‌برداری)
                continue;
            }

            if (!r.Qualifies)
            {
                continue;
            }

            qualified++;

            // دیدوپِ دلال: اگر خودروی هم‌مدل/سال/قیمت/محله در این پاس قبلاً سطح شده، دوباره نساز.
            string fingerprint = $"{vmId}|{listing.ModelYear}|{listing.PriceToman}|{Common.TextNormalizer.Normalize(listing.RawLocation)}";
            if (!seenFingerprints.Add(fingerprint))
            {
                duplicates++;
                qualifyingListingIds.Add(listing.Id); // تا شکارِ موجودِ همین آگهی حذف/فلپ نشود
                continue;
            }

            qualifyingListingIds.Add(listing.Id);

            if (huntByListing.TryGetValue(listing.Id, out Hunt? existing))
            {
                Apply(existing, listing, segment, r, now);
                existing.State = HuntState.Active;
                await _hunts.UpdateAsync(existing, ct);
                updated++;
            }
            else
            {
                var hunt = new Hunt { SourceListingId = listing.Id };
                Apply(hunt, listing, segment, r, now);
                await _hunts.AddAsync(hunt, ct);
                created++;
            }
        }

        // شکارهای پایپ‌لاینی که دیگر واجد شرایط نیستند یا آگهی‌شان حذف شده → Removed.
        foreach (Hunt hunt in pipelineHunts.Where(h => h.State == HuntState.Active))
        {
            if (hunt.SourceListingId is { } sid && !qualifyingListingIds.Contains(sid))
            {
                hunt.State = HuntState.Removed;
                await _hunts.UpdateAsync(hunt, ct);
                removed++;
            }
        }

        return new ScoringRunResult(evaluated, qualified, created, updated, removed, suspicious, duplicates);
    }

    private static void Apply(Hunt hunt, CarListing l, MarketSegmentStat segment, ScoreResult r, DateTimeOffset now)
    {
        hunt.Title = l.Title;
        hunt.ModelYear = l.ModelYear?.ToString() ?? string.Empty;
        hunt.MileageKm = l.MileageKm;
        hunt.Location = l.RawLocation ?? string.Empty;
        hunt.CityId = l.CityId ?? hunt.CityId;
        hunt.PriceToman = l.PriceToman;
        hunt.MarketMedianToman = segment.MedianPriceToman;
        hunt.BelowMarketPercent = r.BelowMarketPercent;
        hunt.Score = r.Score;
        hunt.Liquidity = r.Liquidity;
        hunt.State = HuntState.Active;
        hunt.PublishedAt = l.PublishedAt;
        hunt.IsNew = (now - l.PublishedAt) <= TimeSpan.FromHours(1);
        hunt.AdvertiserPhone = l.AdvertiserPhone;
    }
}
