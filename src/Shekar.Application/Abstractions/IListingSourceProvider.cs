using Shekar.Application.Contracts;
using Shekar.Domain.Enums;

namespace Shekar.Application.Abstractions;

/// <summary>
/// منبع آگهی (دیوار/باما/…). هر پیاده‌سازی آگهی‌های تازه‌ی یک شهر را برمی‌گرداند.
/// در فاز MVP یک پیاده‌سازی ماک داریم؛ بعداً provider واقعیِ کراول HTML جایگزین می‌شود.
/// </summary>
public interface IListingSourceProvider
{
    /// <summary>منبعی که این provider پوشش می‌دهد.</summary>
    ListingSource Source { get; }

    /// <summary>آگهی‌های تازه‌ی یک شهر را دریافت می‌کند.</summary>
    Task<IReadOnlyList<RawListingDto>> FetchRecentAsync(int cityId, CancellationToken ct = default);

    /// <summary>
    /// نسخه‌ی کراول دومرحله‌ای: آگهی‌های تازه را می‌گیرد و فقط آگهی‌هایی که شناسه‌شان در
    /// <paramref name="knownExternalIds"/> نیست (یعنی جدیدند) را با صفحه‌ی جزئیات غنی می‌کند
    /// (سال، مدلِ قطعی، کارکرد، رنگ…). منابعِ بدون جزئیات، همان رفتار پیش‌فرض را دارند.
    /// </summary>
    Task<IReadOnlyList<RawListingDto>> FetchRecentAsync(
        int cityId, IReadOnlySet<string> knownExternalIds, CancellationToken ct = default)
        => FetchRecentAsync(cityId, ct);
}
