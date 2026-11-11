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

    /// <summary>کلید سگمنتِ دقیق (سطح ۰). <paramref name="colorKey"/> از پیش نرمال‌شده است یا NULL.</summary>
    public static string Build(int vehicleModelId, int year, MileageBucket bucket, BodyCondition body, string? colorKey) =>
        $"VM{vehicleModelId}|Y{year}|M{(byte)bucket}|B{(byte)body}|C{colorKey ?? "-"}";

    /// <summary>کلید سطح ۱ (بدونِ رنگ). پیشوندِ «L1|» تضمین می‌کند با کلیدِ دقیق تداخل نکند.</summary>
    public static string BuildL1(int vehicleModelId, int year, MileageBucket bucket, BodyCondition body) =>
        $"L1|VM{vehicleModelId}|Y{year}|M{(byte)bucket}|B{(byte)body}";

    /// <summary>کلید سطح ۲ (بدونِ بدنه و رنگ؛ مدل+سال+کارکرد). پیشوندِ «L2|».</summary>
    public static string BuildL2(int vehicleModelId, int year, MileageBucket bucket) =>
        $"L2|VM{vehicleModelId}|Y{year}|M{(byte)bucket}";

    /// <summary>
    /// کلید سطح ۳ (مدل+سال؛ بدونِ کارکرد/بدنه/رنگ). پیشوندِ «L3|». این سطح همه‌ی کارکردها را گرد می‌آورد
    /// و شیبِ کارکرد روی آن برآورد می‌شود؛ مرجعِ پرنمونه برای مدل‌های کم‌آگهی (با تعدیلِ کارکرد).
    /// </summary>
    public static string BuildL3(int vehicleModelId, int year) =>
        $"L3|VM{vehicleModelId}|Y{year}";

    /// <summary>
    /// کلیدهای سلسله‌مراتبِ یک آگهی، از دقیق به درشت (L0, L1, L2)؛ <c>null</c> اگر مدل/سال نداشته باشد.
    /// اسکورینگ به‌ترتیب امتحان می‌کند و دقیق‌ترین سطحِ قابل‌اتکا را برمی‌دارد.
    /// </summary>
    public static IReadOnlyList<string>? HierarchyKeys(CarListing listing, VehicleModel? model)
    {
        if (listing.VehicleModelId is not { } vmId || listing.ModelYear is not { } year)
        {
            return null;
        }

        MileageBucket bucket = DomainConstants.ResolveMileageBucket(listing.MileageKm);
        string l0 = ForListing(listing, model)!;
        return new[]
        {
            l0,
            BuildL1(vmId, year, bucket, listing.BodyCondition),
            BuildL2(vmId, year, bucket),
            BuildL3(vmId, year)
        };
    }

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
