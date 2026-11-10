using Shekar.Application.Common;
using Shekar.Domain.Entities;

namespace Shekar.Application.Services;

/// <summary>نگاشت متن یک آگهی به یک مدل کاتالوگ (VehicleModel).</summary>
public interface IVehicleModelMatcher
{
    /// <summary>بهترین مدلِ منطبق را برمی‌گرداند یا <c>null</c> اگر تطبیقِ معتبری نبود.</summary>
    int? Match(CarListing listing, IReadOnlyList<VehicleModel> catalog);
}

/// <summary>
/// تطبیقِ هیوریستیکِ fallback (برای آگهی‌هایی که کلیدِ قطعیِ منبع ندارند). متن آگهی را نرمال
/// می‌کند و با برند/مدل/تریم و نام‌های مستعار مقایسه می‌کند، اما:
/// • تطبیق با <b>مرزِ کلمه</b> انجام می‌شود تا «۲۰۶» با «۲۰۶۱»/«۱۲۰۶» یا داخلِ اعداد دیگر اشتباه نشود؛
/// • تطبیقِ <b>مدل</b> (یا alias همراه تریم) لازم است؛ تطبیقِ صرفِ alias یا برند کافی نیست؛
/// • دقیق‌ترین/تریم‌دارترین گزینه انتخاب می‌شود و در ابهام، <c>null</c> برمی‌گردد.
/// هیچ مدلی نمی‌سازد؛ فقط به مدل‌های موجود نگاشت می‌دهد (منبعِ هویت، رجیستر خودکارِ قطعی است).
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

            bool modelHit = ContainsWord(haystack, model);
            bool brandHit = brand.Length == 0 || ContainsWord(haystack, brand);
            bool trimHit = ContainsWord(haystack, trim);
            bool aliasHit = vm.Aliases.Any(a => ContainsWord(haystack, TextNormalizer.Normalize(a)));

            // شرطِ اعتماد: یا مدل مستقیماً هست، یا alias همراهِ تریم (نه alias/برندِ تنها).
            bool qualifies = modelHit || (aliasHit && trimHit);
            if (!qualifies)
            {
                continue;
            }

            int score = modelHit
                ? 3 + (brandHit ? 1 : 0) + (trimHit ? 2 : 0) + model.Length
                : 2 + (trimHit ? 1 : 0); // مسیرِ alias+trim

            if (score > bestScore)
            {
                bestScore = score;
                bestId = vm.Id;
            }
        }

        return bestId;
    }

    /// <summary>آیا <paramref name="needle"/> به‌صورت یک «کلمه‌ی کامل» در haystack هست (نه داخلِ کلمه/عددِ بزرگ‌تر).</summary>
    private static bool ContainsWord(string haystack, string needle)
    {
        if (string.IsNullOrEmpty(needle))
        {
            return false;
        }

        int idx = 0;
        while ((idx = haystack.IndexOf(needle, idx, StringComparison.Ordinal)) >= 0)
        {
            bool leftOk = idx == 0 || !IsWordChar(haystack[idx - 1]);
            int end = idx + needle.Length;
            bool rightOk = end == haystack.Length || !IsWordChar(haystack[end]);
            if (leftOk && rightOk)
            {
                return true;
            }
            idx = end;
        }
        return false;
    }

    private static bool IsWordChar(char c) => char.IsLetterOrDigit(c);
}
