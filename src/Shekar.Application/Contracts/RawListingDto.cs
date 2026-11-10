using Shekar.Domain.Enums;

namespace Shekar.Application.Contracts;

/// <summary>آگهی خامِ دریافت‌شده از یک منبع (دیوار/باما)، پیش از فیلتر و ذخیره.</summary>
public sealed record RawListingDto
{
    public ListingSource Source { get; init; }
    public string ExternalId { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }

    public string? RawBrand { get; init; }
    public string? RawModel { get; init; }
    public string? RawTrim { get; init; }

    /// <summary>
    /// شناسه‌ی قطعیِ مدل در منبع (مثلاً brand_model دیوار: «Peugeot 206 3-normal»).
    /// مبنای رجیستر خودکار مدل در کاتالوگ برای پوشش همه‌ی مدل‌ها.
    /// </summary>
    public string? SourceModelKey { get; init; }

    public int? ModelYear { get; init; }
    public int? MileageKm { get; init; }
    public string? Color { get; init; }
    public BodyCondition BodyCondition { get; init; } = BodyCondition.Unknown;

    public long PriceToman { get; init; }
    public int? CityId { get; init; }
    public string? RawLocation { get; init; }
    public string? AdvertiserPhone { get; init; }

    public int ImageCount { get; init; }
    public string? FirstImageUrl { get; init; }
    public DateTimeOffset PublishedAt { get; init; }
    public string? RawJson { get; init; }
}
