using Shekar.Application.Abstractions;
using Shekar.Domain.Entities;

namespace Shekar.Application.Services;

/// <summary>نرمال‌سازی آگهی‌های خام: نگاشت به VehicleModelId.</summary>
public interface IVehicleNormalizationService
{
    /// <summary>تا سقف مشخص، آگهی‌های نرمال‌نشده را تطبیق/رجیستر می‌دهد و تعداد تطبیق‌شده را برمی‌گرداند.</summary>
    Task<int> RunAsync(int maxBatch, CancellationToken ct = default);
}

/// <summary>
/// نگاشت آگهی به مدل، به دو روش:
/// (۱) <b>قطعی</b> — اگر آگهی <c>SourceModelKey</c> (brand_model دیوار) داشته باشد، مدلِ متناظر
/// یافته یا <b>خودکار ساخته</b> می‌شود؛ این یعنی پوشش <b>همه‌ی مدل‌ها</b> بدون فهرست دستی.
/// (۲) <b>هیوریستیک</b> — در غیر این صورت، متن آگهی با کاتالوگ تطبیق داده می‌شود (fallback).
/// </summary>
public sealed class VehicleNormalizationService : IVehicleNormalizationService
{
    /// <summary>نقدشوندگیِ پیش‌فرضِ مدلِ خودکارساخته‌شده تا وقتی ادمین تنظیم کند (خنثی = متوسط).</summary>
    private const int DefaultLiquidityScore = 50;

    private readonly ICarListingRepository _listings;
    private readonly IVehicleModelRepository _models;
    private readonly IVehicleModelMatcher _matcher;

    public VehicleNormalizationService(
        ICarListingRepository listings, IVehicleModelRepository models, IVehicleModelMatcher matcher)
    {
        _listings = listings;
        _models = models;
        _matcher = matcher;
    }

    public async Task<int> RunAsync(int maxBatch, CancellationToken ct = default)
    {
        IReadOnlyList<CarListing> pending = await _listings.ListUnmatchedAsync(maxBatch, ct);
        if (pending.Count == 0)
        {
            return 0;
        }

        // کاتالوگِ فعلی را به‌عنوان کش بارگذاری می‌کنیم؛ مدل‌های خودکار در همین پاس به آن افزوده می‌شوند.
        List<VehicleModel> catalog = (await _models.ListActiveAsync(ct)).ToList();
        var byDivarKey = catalog
            .Where(m => !string.IsNullOrEmpty(m.DivarKey))
            .GroupBy(m => m.DivarKey!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        int matched = 0;

        foreach (CarListing listing in pending)
        {
            ct.ThrowIfCancellationRequested();

            int? vmId = null;

            // (۱) مسیر قطعی: شناسه‌ی منبع.
            if (!string.IsNullOrWhiteSpace(listing.SourceModelKey))
            {
                VehicleModel model = await ResolveOrCreateAsync(listing, byDivarKey, catalog, ct);
                vmId = model.Id;
            }
            else if (catalog.Count > 0)
            {
                // (۲) مسیر هیوریستیک: تطبیق متن با کاتالوگ.
                vmId = _matcher.Match(listing, catalog);
            }

            if (vmId is not null)
            {
                listing.VehicleModelId = vmId;
                await _listings.UpdateAsync(listing, ct);
                matched++;
            }
        }

        return matched;
    }

    private async Task<VehicleModel> ResolveOrCreateAsync(
        CarListing listing, Dictionary<string, VehicleModel> byDivarKey, List<VehicleModel> catalog, CancellationToken ct)
    {
        string key = listing.SourceModelKey!.Trim();

        if (byDivarKey.TryGetValue(key, out VehicleModel? cached))
        {
            return cached;
        }

        VehicleModel? existing = await _models.GetByDivarKeyAsync(key, ct);
        if (existing is not null)
        {
            byDivarKey[key] = existing;
            catalog.Add(existing);
            return existing;
        }

        // رجیستر خودکار: برند/مدل/تریم از دادهٔ آگهی، نقدشوندگیِ پیش‌فرض تا تنظیمِ ادمین.
        var created = new VehicleModel
        {
            DivarKey = key,
            Brand = listing.RawBrand ?? string.Empty,
            Model = string.IsNullOrWhiteSpace(listing.RawModel) ? key : listing.RawModel!,
            Trim = listing.RawTrim,
            LiquidityScore = DefaultLiquidityScore,
            Aliases = Array.Empty<string>(),
            ColorSensitive = false,
            IsActive = true,
            Notes = "رجیستر خودکار از دیوار"
        };
        VehicleModel saved = await _models.AddAsync(created, ct);
        byDivarKey[key] = saved;
        catalog.Add(saved);
        return saved;
    }
}
