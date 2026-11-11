using Shekar.Application.Common;
using Shekar.Domain.Entities;

namespace Shekar.Application.Services;

/// <summary>نگاشت متن یک آگهی به یک مدل کاتالوگ (VehicleModel).</summary>
public interface IVehicleModelMatcher
{
    /// <summary>بهترین مدلِ منطبق را برمی‌گرداند یا <c>null</c> اگر تطبیقِ معتبری نبود (راحتی؛ کاتالوگ را هر بار نرمال می‌کند).</summary>
    int? Match(CarListing listing, IReadOnlyList<VehicleModel> catalog);

    /// <summary>تطبیق روی کاتالوگِ <b>از پیش‌نرمال‌شده</b> (مسیرِ داغِ حلقه‌ی نرمال‌سازی).</summary>
    int? Match(CarListing listing, NormalizedCatalog catalog);

    /// <summary>کاتالوگ را یک‌بار نرمال می‌کند تا در چند تطبیقِ پیاپی دوباره‌کاری نشود.</summary>
    NormalizedCatalog Prepare(IReadOnlyList<VehicleModel> catalog);
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
    public NormalizedCatalog Prepare(IReadOnlyList<VehicleModel> catalog) => new(catalog);

    public int? Match(CarListing listing, IReadOnlyList<VehicleModel> catalog) => Match(listing, Prepare(catalog));

    public int? Match(CarListing listing, NormalizedCatalog catalog)
    {
        string haystack = TextNormalizer.Normalize(
            $"{listing.RawBrand} {listing.RawModel} {listing.RawTrim} {listing.Title}");
        if (haystack.Length == 0)
        {
            return null;
        }

        int? bestId = null;
        int bestScore = 0;

        foreach (NormalizedModel vm in catalog.Entries)
        {
            bool modelHit = ContainsWord(haystack, vm.Model);
            bool brandHit = vm.Brand.Length == 0 || ContainsWord(haystack, vm.Brand);
            bool trimHit = ContainsWord(haystack, vm.Trim);
            bool aliasHit = AnyAliasHit(haystack, vm.Aliases);

            // شرطِ اعتماد: یا مدل مستقیماً هست، یا alias همراهِ تریم (نه alias/برندِ تنها).
            bool qualifies = modelHit || (aliasHit && trimHit);
            if (!qualifies)
            {
                continue;
            }

            int score = modelHit
                ? 3 + (brandHit ? 1 : 0) + (trimHit ? 2 : 0) + vm.Model.Length
                : 2 + (trimHit ? 1 : 0); // مسیرِ alias+trim

            if (score > bestScore)
            {
                bestScore = score;
                bestId = vm.Id;
            }
        }

        return bestId;
    }

    private static bool AnyAliasHit(string haystack, string[] aliases)
    {
        foreach (string a in aliases)
        {
            if (ContainsWord(haystack, a))
            {
                return true;
            }
        }
        return false;
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

/// <summary>یک ردیفِ کاتالوگ با فیلدهای از پیش‌نرمال‌شده (برای تطبیقِ ارزان).</summary>
public readonly record struct NormalizedModel(int Id, string Brand, string Model, string Trim, string[] Aliases);

/// <summary>
/// کاتالوگِ از پیش‌نرمال‌شده؛ یک‌بار از روی مدل‌ها ساخته می‌شود و با رجیسترِ خودکار قابلِ گسترش است،
/// تا در حلقه‌ی نرمال‌سازی، متنِ هر مدل برای هر آگهی دوباره نرمال نشود.
/// </summary>
public sealed class NormalizedCatalog
{
    private readonly List<NormalizedModel> _entries;

    public NormalizedCatalog(IReadOnlyList<VehicleModel> models)
    {
        _entries = new List<NormalizedModel>(models.Count);
        foreach (VehicleModel m in models)
        {
            _entries.Add(Create(m));
        }
    }

    internal IReadOnlyList<NormalizedModel> Entries => _entries;

    /// <summary>مدلِ تازه (مثلاً رجیسترِ خودکار) را به کاتالوگِ نرمال‌شده می‌افزاید.</summary>
    public void Add(VehicleModel model) => _entries.Add(Create(model));

    private static NormalizedModel Create(VehicleModel vm) => new(
        vm.Id,
        TextNormalizer.Normalize(vm.Brand),
        TextNormalizer.Normalize(vm.Model),
        TextNormalizer.Normalize(vm.Trim),
        vm.Aliases.Select(TextNormalizer.Normalize).Where(a => a.Length > 0).ToArray());
}
