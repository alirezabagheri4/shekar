using Shekar.Application.Abstractions;
using Shekar.Domain.Entities;

namespace Shekar.Application.Services;

/// <summary>نرمال‌سازی آگهی‌های خام: نگاشت به VehicleModelId با استفاده از کاتالوگ.</summary>
public interface IVehicleNormalizationService
{
    /// <summary>تا سقف مشخص، آگهی‌های نرمال‌نشده را تطبیق می‌دهد و تعداد تطبیق‌شده را برمی‌گرداند.</summary>
    Task<int> RunAsync(int maxBatch, CancellationToken ct = default);
}

public sealed class VehicleNormalizationService : IVehicleNormalizationService
{
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
        IReadOnlyList<VehicleModel> catalog = await _models.ListActiveAsync(ct);
        if (catalog.Count == 0)
        {
            return 0;
        }

        IReadOnlyList<CarListing> pending = await _listings.ListUnmatchedAsync(maxBatch, ct);
        int matched = 0;

        foreach (CarListing listing in pending)
        {
            ct.ThrowIfCancellationRequested();
            int? vmId = _matcher.Match(listing, catalog);
            if (vmId is not null)
            {
                listing.VehicleModelId = vmId;
                await _listings.UpdateAsync(listing, ct);
                matched++;
            }
        }

        return matched;
    }
}
