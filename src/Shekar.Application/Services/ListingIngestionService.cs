using Shekar.Application.Abstractions;
using Shekar.Application.Common;
using Shekar.Application.Contracts;
using Shekar.Domain.Entities;
using Shekar.Domain.Enums;

namespace Shekar.Application.Services;

/// <summary>نتیجه‌ی یک پاس ingestion.</summary>
public sealed record IngestionResult(int Fetched, int Saved, int Skipped, int Removed, int Errors);

/// <summary>دریافت و ذخیره‌ی آگهی‌های خام با فیلترها (مرحله ۱ پایپ‌لاین).</summary>
public interface IListingIngestionService
{
    Task<IngestionResult> RunAsync(IListingSourceProvider provider, int cityId, int maxAgeHours, int staleAfterHours, CancellationToken ct = default);
}

/// <summary>
/// منطق ingestion: فیلتر (۲۴ ساعت اخیر، عکس‌دار، قیمت معتبر، بدون‌تکرار، شماره‌ی بلاک‌نشده)،
/// ذخیره‌ی آگهی تازه، به‌روزرسانی آگهی موجود، و تشخیص آگهی‌های حذف‌شده. لاگ در CrawlRuns.
/// </summary>
public sealed class ListingIngestionService : IListingIngestionService
{
    private readonly ICarListingRepository _listings;
    private readonly ICrawlRunRepository _runs;
    private readonly IBlockedAdvertiserRepository _blocked;
    private readonly IClock _clock;

    public ListingIngestionService(
        ICarListingRepository listings,
        ICrawlRunRepository runs,
        IBlockedAdvertiserRepository blocked,
        IClock clock)
    {
        _listings = listings;
        _runs = runs;
        _blocked = blocked;
        _clock = clock;
    }

    public async Task<IngestionResult> RunAsync(IListingSourceProvider provider, int cityId, int maxAgeHours, int staleAfterHours, CancellationToken ct = default)
    {
        DateTimeOffset now = _clock.UtcNow;
        CrawlRun run = await _runs.AddAsync(new CrawlRun
        {
            Source = provider.Source,
            CityId = cityId,
            StartedAt = now,
            Status = CrawlStatus.Running
        }, ct);

        int fetched = 0, saved = 0, skipped = 0, removed = 0, errors = 0;

        try
        {
            // آگهی‌های فعالِ این منبع را یک‌بار بارگذاری می‌کنیم: هم برای رد کردنِ فچِ تکراریِ
            // صفحه‌ی جزئیات (کراول دومرحله‌ای)، هم برای تشخیصِ کهنگی در پایان پاس.
            IReadOnlyList<CarListing> active = await _listings.ListActiveAsync(provider.Source, ct);
            // فقط آگهی‌های «غنی‌شده» (دارای سال) را شناخته‌شده می‌شماریم تا صفحه‌ی جزئیاتشان
            // دوباره فچ نشود. آگهیِ موجودِ بدون‌سال (مثلاً فچِ اول چلنجِ ضدبات خورده) دوباره تلاش می‌شود.
            var enrichedExternalIds = active
                .Where(l => l.ModelYear is not null)
                .Select(l => l.ExternalId)
                .ToHashSet(StringComparer.Ordinal);

            IReadOnlyList<RawListingDto> ads = await provider.FetchRecentAsync(cityId, enrichedExternalIds, ct);
            DateTimeOffset minPublished = now.AddHours(-maxAgeHours);
            var seenExternalIds = new HashSet<string>(StringComparer.Ordinal);

            foreach (RawListingDto ad in ads)
            {
                ct.ThrowIfCancellationRequested();
                fetched++;
                seenExternalIds.Add(ad.ExternalId);

                try
                {
                    // فیلتر ۱: فقط ۲۴ ساعت اخیر.
                    if (ad.PublishedAt < minPublished)
                    {
                        skipped++;
                        continue;
                    }

                    // فیلتر ۲: حتماً عکس.
                    if (ad.ImageCount <= 0)
                    {
                        skipped++;
                        continue;
                    }

                    // فیلتر ۳: قیمت معتبر.
                    if (ad.PriceToman <= 0)
                    {
                        skipped++;
                        continue;
                    }

                    // فیلتر ۴: شماره‌ی بلاک‌شده (دلال/اسپم).
                    if (!string.IsNullOrEmpty(ad.AdvertiserPhone)
                        && await _blocked.IsBlockedAsync(ad.AdvertiserPhone, now, ct))
                    {
                        skipped++;
                        continue;
                    }

                    // تکراری دقیق: همان آگهی از همان منبع → فقط LastSeen به‌روز شود.
                    CarListing? existing = await _listings.GetByExternalIdAsync(ad.Source, ad.ExternalId, ct);
                    if (existing is not null)
                    {
                        existing.LastSeenAt = now;
                        existing.PriceToman = ad.PriceToman; // ممکن است قیمت به‌روز شده باشد
                        if (existing.State == ListingState.Removed)
                        {
                            existing.State = ListingState.Active;
                            existing.RemovedAt = null;
                        }
                        BackfillEnrichment(existing, ad);
                        await _listings.UpdateAsync(existing, ct);
                        continue;
                    }

                    // تکراری نزدیک: شماره+عنوان+قیمت یکسان.
                    string hash = ContentHash.Compute(ad.AdvertiserPhone, ad.Title, ad.PriceToman);
                    if (await _listings.ExistsActiveByContentHashAsync(hash, ct))
                    {
                        skipped++;
                        continue;
                    }

                    await _listings.AddAsync(Map(ad, hash, cityId, now), ct);
                    saved++;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch
                {
                    errors++;
                }
            }

            // تشخیص حذف با TTLِ کهنگی: چون کراولِ تدریجی فقط آگهی‌های تازه را اسکن می‌کند،
            // «دیده‌نشدن در این پاس» دلیل حذف نیست. فقط آگهیِ فعالی که مدتی طولانی (StaleAfterHours)
            // دیگر دیده نشده را حذف‌شده علامت می‌زنیم. (آگهی‌های دیده‌شده در این پاس LastSeen=now دارند.)
            DateTimeOffset staleBefore = now.AddHours(-Math.Max(1, staleAfterHours));
            foreach (CarListing listing in active.Where(l =>
                l.CityId == cityId && !seenExternalIds.Contains(l.ExternalId) && l.LastSeenAt < staleBefore))
            {
                listing.State = ListingState.Removed;
                listing.RemovedAt = now;
                await _listings.UpdateAsync(listing, ct);
                removed++;
            }

            run.Status = CrawlStatus.Succeeded;
        }
        catch (Exception ex)
        {
            run.Status = CrawlStatus.Failed;
            run.Error = ex.Message;
            errors++;
        }

        run.FinishedAt = _clock.UtcNow;
        run.FetchedCount = fetched;
        run.SavedCount = saved;
        run.SkippedCount = skipped;
        run.RemovedCount = removed;
        run.ErrorCount = errors;
        await _runs.UpdateAsync(run, ct);

        return new IngestionResult(fetched, saved, skipped, removed, errors);
    }

    /// <summary>
    /// فیلدهای غنی‌سازی را روی آگهیِ موجود بک‌فیل می‌کند، فقط وقتی قبلاً خالی بوده‌اند و اکنون
    /// از جزئیات آمده‌اند (بازیابی پس از چلنجِ ضدباتِ فچِ اول). قیمت اینجا دست نمی‌خورد.
    /// </summary>
    private static void BackfillEnrichment(CarListing existing, RawListingDto ad)
    {
        if (existing.ModelYear is null && ad.ModelYear is not null) existing.ModelYear = ad.ModelYear;
        if (existing.MileageKm is null && ad.MileageKm is not null) existing.MileageKm = ad.MileageKm;
        if (string.IsNullOrEmpty(existing.Color) && !string.IsNullOrEmpty(ad.Color)) existing.Color = ad.Color;
        if (string.IsNullOrEmpty(existing.SourceModelKey) && !string.IsNullOrEmpty(ad.SourceModelKey)) existing.SourceModelKey = ad.SourceModelKey;
        if (string.IsNullOrEmpty(existing.RawBrand) && !string.IsNullOrEmpty(ad.RawBrand)) existing.RawBrand = ad.RawBrand;
        if (string.IsNullOrEmpty(existing.RawModel) && !string.IsNullOrEmpty(ad.RawModel)) existing.RawModel = ad.RawModel;
        if (string.IsNullOrEmpty(existing.RawTrim) && !string.IsNullOrEmpty(ad.RawTrim)) existing.RawTrim = ad.RawTrim;
    }

    private static CarListing Map(RawListingDto ad, string hash, int cityId, DateTimeOffset now) => new()
    {
        Source = ad.Source,
        ExternalId = ad.ExternalId,
        Url = ad.Url,
        Title = ad.Title,
        Description = ad.Description,
        RawBrand = ad.RawBrand,
        RawModel = ad.RawModel,
        RawTrim = ad.RawTrim,
        SourceModelKey = ad.SourceModelKey,
        ModelYear = ad.ModelYear,
        MileageKm = ad.MileageKm,
        Color = ad.Color,
        BodyCondition = ad.BodyCondition,
        PriceToman = ad.PriceToman,
        CityId = ad.CityId ?? cityId,
        RawLocation = ad.RawLocation,
        AdvertiserPhone = ad.AdvertiserPhone,
        ImageCount = ad.ImageCount,
        FirstImageUrl = ad.FirstImageUrl,
        PublishedAt = ad.PublishedAt,
        FetchedAt = now,
        LastSeenAt = now,
        State = ListingState.Active,
        IsScored = false,
        ContentHash = hash,
        RawJson = ad.RawJson
    };
}
