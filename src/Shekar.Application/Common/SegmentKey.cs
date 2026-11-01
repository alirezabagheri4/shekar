using Shekar.Domain;
using Shekar.Domain.Entities;
using Shekar.Domain.Enums;

namespace Shekar.Application.Common;

/// <summary>سازنده‌ی کلید سگمنت بازار؛ در اگریگیشن و اسکورینگ به‌طور یکسان استفاده می‌شود.</summary>
public static class SegmentKey
{
    /// <summary>آیا رنگ باید در سگمنت دخیل شود (قیمت &gt; ۴ میلیارد یا مدلِ حساس به رنگ).</summary>
    public static bool ColorInKey(long priceToman, VehicleModel? model) =>
        priceToman > DomainConstants.ColorMattersAboveToman || (model?.ColorSensitive ?? false);

    /// <summary>کلید سگمنت را می‌سازد. <paramref name="colorKey"/> از پیش نرمال‌شده است یا NULL.</summary>
    public static string Build(int vehicleModelId, int year, MileageBucket bucket, BodyCondition body, string? colorKey) =>
        $"VM{vehicleModelId}|Y{year}|M{(byte)bucket}|B{(byte)body}|C{colorKey ?? "-"}";

    /// <summary>کلید سگمنت یک آگهی را می‌سازد؛ <c>null</c> اگر مدل یا سال نداشته باشد.</summary>
    public static string? ForListing(CarListing listing, VehicleModel? model)
    {
        if (listing.VehicleModelId is not { } vmId || listing.ModelYear is not { } year)
        {
            return null;
        }

        string? colorKey = ColorInKey(listing.PriceToman, model) ? TextNormalizer.Normalize(listing.Color) : null;
        if (colorKey is { Length: 0 })
        {
            colorKey = null;
        }

        return Build(vmId, year, DomainConstants.ResolveMileageBucket(listing.MileageKm), listing.BodyCondition, colorKey);
    }
}
