using Shekar.Application.Abstractions;
using Shekar.Application.Contracts;
using Shekar.Domain.Enums;

namespace Shekar.Mock.Providers;

/// <summary>
/// منبع آگهی ماک برای توسعه‌ی پایپ‌لاین بدون کراول واقعی.
/// داده را «خوشه‌ای» تولید می‌کند: چند مدل با چند نمونه (تا سگمنت نمونه‌ی کافی بگیرد) و
/// چند «معامله‌ی زیرقیمت» تا اسکورینگ تاپ واقعی بسازد. چند آگهی ناقص هم برای آزمودن فیلترها.
/// </summary>
public sealed class MockListingSourceProvider : IListingSourceProvider
{
    // هر خوشه: برند، مدل، تریم، سال، کارکردِ پایه (هم‌باکت)، قیمتِ میانه‌ی بازار.
    private static readonly (string Brand, string Model, string Trim, int Year, int Mileage, long Market)[] Clusters =
    {
        ("پژو", "۲۰۷", "دستی", 1402, 15_000, 1_560_000_000),
        ("ایران‌خودرو", "دنا", "پلاس توربو", 1401, 40_000, 2_600_000_000),
        ("سایپا", "کوییک", "اتوماتیک", 1402, 25_000, 1_100_000_000)
    };
    private static readonly string[] Colors = { "سفید", "مشکی", "خاکستری", "نقره‌ای" };

    private readonly IClock _clock;
    private int _seq;

    public MockListingSourceProvider(IClock clock) => _clock = clock;

    public ListingSource Source => ListingSource.Divar;

    public Task<IReadOnlyList<RawListingDto>> FetchRecentAsync(int cityId, CancellationToken ct = default)
    {
        var rnd = Random.Shared;
        DateTimeOffset now = _clock.UtcNow;
        var list = new List<RawListingDto>();

        foreach (var (brand, model, trim, year, mileage, market) in Clusters)
        {
            // چهار آگهیِ «نرمال» نزدیک میانه (±۳٪) و هم‌باکت کارکرد.
            for (int i = 0; i < 4; i++)
            {
                long price = market + (long)(market * (rnd.NextDouble() * 0.06 - 0.03));
                list.Add(Make(cityId, now, brand, model, trim, year, mileage + rnd.Next(-2000, 2000), price));
            }

            // یک «معامله»: ~۱۸٪ زیر بازار → باید تاپ شود.
            list.Add(Make(cityId, now, brand, model, trim, year, mileage, (long)(market * 0.82)));
        }

        // دو آگهی ناقص برای آزمودن فیلترها (قدیمی و بی‌عکس).
        list.Add(Make(cityId, now, "پژو", "۲۰۷", "دستی", 1402, 15_000, 1_500_000_000, hoursAgo: 48));
        list.Add(Make(cityId, now, "سایپا", "کوییک", "اتوماتیک", 1402, 25_000, 1_050_000_000, noPhoto: true));

        return Task.FromResult<IReadOnlyList<RawListingDto>>(list);
    }

    private RawListingDto Make(int cityId, DateTimeOffset now, string brand, string model, string trim,
        int year, int mileage, long price, double hoursAgo = 0, bool noPhoto = false)
    {
        var rnd = Random.Shared;
        string externalId = $"MK-{cityId}-{now:yyyyMMddHH}-{++_seq}";
        double ago = hoursAgo > 0 ? hoursAgo : rnd.NextDouble() * 12;

        return new RawListingDto
        {
            Source = Source,
            ExternalId = externalId,
            Url = $"https://divar.ir/v/{externalId}",
            Title = $"{brand} {model} {trim} مدل {year}",
            Description = "خودرو تمیز، بیمه معتبر.",
            RawBrand = brand,
            RawModel = model,
            RawTrim = trim,
            ModelYear = year,
            MileageKm = Math.Max(0, mileage),
            Color = Colors[rnd.Next(Colors.Length)],
            BodyCondition = BodyCondition.Healthy,
            PriceToman = price,
            CityId = cityId,
            RawLocation = "تهرانپارس",
            AdvertiserPhone = $"0912{rnd.Next(1000000, 9999999)}",
            ImageCount = noPhoto ? 0 : rnd.Next(1, 12),
            FirstImageUrl = noPhoto ? null : $"https://img/{externalId}.jpg",
            PublishedAt = now.AddHours(-ago),
            RawJson = null
        };
    }
}
