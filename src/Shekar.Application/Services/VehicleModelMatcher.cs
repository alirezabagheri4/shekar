using Shekar.Application.Common;
using Shekar.Domain.Entities;

namespace Shekar.Application.Services;

/// <summary>نگاشت متن یک آگهی به یک مدل کاتالوگ (VehicleModel).</summary>
public interface IVehicleModelMatcher
{
    /// <summary>بهترین مدلِ منطبق را برمی‌گرداند یا <c>null</c> اگر تطبیقی نبود.</summary>
    int? Match(CarListing listing, IReadOnlyList<VehicleModel> catalog);
}

/// <summary>
/// تطبیق هیوریستیک: متن آگهی (برند/مدل/تریم/عنوان) را نرمال می‌کند و با برند+مدل (+تریم)
/// و نام‌های مستعار هر مدل مقایسه می‌کند. تریم‌دارترین و دقیق‌ترین تطبیق را برمی‌گزیند.
/// </summary>
public sealed class VehicleModelMatcher : IVehicleModelMatcher
{
    public int? Match(CarListing listing, IReadOnlyList<VehicleModel> catalog)
    {
        string haystack = TextNormalizer.Normalize(
            $"{listing.RawBrand} {listing.RawModel} {listing.RawTrim} {listing.Title}");
        if (haystack.Length == 0)
        {
            return null;
        }

        int? bestId = null;
        int bestScore = 0;

        foreach (VehicleModel vm in catalog)
        {
            string brand = TextNormalizer.Normalize(vm.Brand);
            string model = TextNormalizer.Normalize(vm.Model);
            string trim = TextNormalizer.Normalize(vm.Trim);

            bool modelHit = model.Length > 0 && haystack.Contains(model, StringComparison.Ordinal);
            bool brandHit = brand.Length == 0 || haystack.Contains(brand, StringComparison.Ordinal);
            bool trimHit = trim.Length > 0 && haystack.Contains(trim, StringComparison.Ordinal);
            bool aliasHit = vm.Aliases.Any(a =>
            {
                string na = TextNormalizer.Normalize(a);
                return na.Length > 0 && haystack.Contains(na, StringComparison.Ordinal);
            });

            int score = 0;
            if (modelHit)
            {
                // تطبیق مدل کافی است (برند در آگهی‌ها اغلب حذف می‌شود)؛ برند/تریم/طولِ مدل امتیاز بیشتر.
                score = 2 + (brandHit ? 1 : 0) + (trimHit ? 2 : 0) + model.Length;
            }
            else if (aliasHit)
            {
                score = 1 + (trimHit ? 1 : 0);
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestId = vm.Id;
            }
        }

        return bestId;
    }
}
