using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shekar.Application.Abstractions;
using Shekar.Application.Contracts;
using Shekar.Domain.Enums;

namespace Shekar.Infrastructure.Crawling;

/// <summary>
/// منبع آگهیِ واقعیِ دیوار (کراول HTML صفحه‌ی دسته‌ی خودرو و استخراج JSON تعبیه‌شده).
/// تمام درخواست‌ها از طریق <see cref="IWebFetcher"/> با رفتار انسان‌مانند و ضدبلاک انجام می‌شود؛
/// فقط چند صفحه‌ی اول (آگهی‌های تازه) خوانده می‌شود و صفحه‌ی جزئیات برای کاهش بار فچ نمی‌شود.
///
/// نکته: ساختار صفحه‌ی دیوار ممکن است تغییر کند؛ منطق استخراج دفاعی است و در اولین اجرای واقعی
/// باید نام فیلدها/الگوی اسکریپت تعبیه‌شده تأیید و در صورت نیاز تنظیم شود (لاگِ «۰ آگهی»).
/// </summary>
public sealed partial class DivarHtmlProvider : IListingSourceProvider
{
    private const string BaseUrl = "https://divar.ir";

    private readonly IWebFetcher _fetcher;
    private readonly PoliteFetchOptions _options;
    private readonly ILogger<DivarHtmlProvider> _logger;

    public DivarHtmlProvider(IWebFetcher fetcher, IOptions<PoliteFetchOptions> options, ILogger<DivarHtmlProvider> logger)
    {
        _fetcher = fetcher;
        _options = options.Value;
        _logger = logger;
    }

    public ListingSource Source => ListingSource.Divar;

    public async Task<IReadOnlyList<RawListingDto>> FetchRecentAsync(int cityId, CancellationToken ct = default)
    {
        if (!_options.CitySlugs.TryGetValue(cityId, out string? slug))
        {
            _logger.LogWarning("اسلاگ دیوار برای شهر {City} تعریف نشده", cityId);
            return Array.Empty<RawListingDto>();
        }

        var results = new List<RawListingDto>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        using IFetchSession session = _fetcher.StartSession(referer: $"{BaseUrl}/s/{slug}");

        for (int page = 0; page < _options.MaxPagesPerRun; page++)
        {
            // URL فیلتردار کاربر؛ برای صفحات بعدی page=N به فیلترها اضافه می‌شود.
            string filterQuery = string.IsNullOrWhiteSpace(_options.FilterQuery)
                ? string.Empty
                : _options.FilterQuery.TrimStart('?', '&');
            string url = page == 0
                ? (string.IsNullOrEmpty(filterQuery)
                    ? $"{BaseUrl}/s/{slug}/car"
                    : $"{BaseUrl}/s/{slug}/car?{filterQuery}")
                : (string.IsNullOrEmpty(filterQuery)
                    ? $"{BaseUrl}/s/{slug}/car?page={page + 1}"
                    : $"{BaseUrl}/s/{slug}/car?{filterQuery}&page={page + 1}");

            string? html = await session.GetHtmlAsync(url, ct);
            if (html is null)
            {
                break; // بودجه/بلاک/خطا — همین‌جا متوقف شو
            }

            int before = results.Count;
            foreach (RawListingDto dto in ParsePage(html, slug))
            {
                if (seen.Add(dto.ExternalId))
                {
                    results.Add(dto);
                }
            }

            if (results.Count == before)
            {
                break; // صفحه‌ی خالی یا الگوی تغییریافته → ادامه نده
            }
        }

        _logger.LogInformation("دیوار/{Slug}: {Count} آگهی استخراج شد", slug, results.Count);
        return results;
    }

    /// <summary>از HTML، JSON تعبیه‌شده را درمی‌آورد و کارت‌های آگهی را نگاشت می‌کند.</summary>
    private IEnumerable<RawListingDto> ParsePage(string html, string slug)
    {
        string? json = ExtractEmbeddedJson(html);
        if (json is null)
        {
            yield break;
        }

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(json);
        }
        catch (JsonException)
        {
            yield break;
        }

        using (doc)
        {
            foreach (JsonElement post in FindPostObjects(doc.RootElement))
            {
                RawListingDto? dto = MapPost(post, slug);
                if (dto is not null)
                {
                    yield return dto;
                }
            }
        }
    }

    /// <summary>اسکریپت وضعیتِ اولیه را از HTML استخراج می‌کند (الگوهای رایج Next.js/پری‌لود).</summary>
    private static string? ExtractEmbeddedJson(string html)
    {
        Match next = NextDataRegex().Match(html);
        if (next.Success)
        {
            return next.Groups[1].Value;
        }

        Match preload = PreloadRegex().Match(html);
        return preload.Success ? preload.Groups[1].Value : null;
    }

    /// <summary>به‌صورت بازگشتی دنبال آرایه‌ای از اشیاءِ شبیه‌آگهی (دارای token/title) می‌گردد.</summary>
    private static IEnumerable<JsonElement> FindPostObjects(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Array:
                bool looksLikePosts = element.GetArrayLength() > 0
                    && element[0].ValueKind == JsonValueKind.Object
                    && (element[0].TryGetProperty("token", out _) || element[0].TryGetProperty("title", out _));
                if (looksLikePosts)
                {
                    foreach (JsonElement item in element.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.Object)
                        {
                            yield return item;
                        }
                    }
                }
                else
                {
                    foreach (JsonElement item in element.EnumerateArray())
                    {
                        foreach (JsonElement found in FindPostObjects(item))
                        {
                            yield return found;
                        }
                    }
                }
                break;

            case JsonValueKind.Object:
                foreach (JsonProperty prop in element.EnumerateObject())
                {
                    foreach (JsonElement found in FindPostObjects(prop.Value))
                    {
                        yield return found;
                    }
                }
                break;
        }
    }

    private RawListingDto? MapPost(JsonElement post, string slug)
    {
        string? token = GetString(post, "token");
        string? title = GetString(post, "title") ?? GetString(post, "normalizedTitle");
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(title))
        {
            return null;
        }

        long price = ReadPrice(post);
        string? image = GetString(post, "imageUrl") ?? GetString(post, "image") ?? FirstImageFromArray(post);
        int imageCount = image is null ? 0 : 1;
        string? subtitle = GetString(post, "subtitle") ?? GetString(post, "middleDescriptionText");
        (int? year, int? mileage) = ParseYearMileage($"{title} {subtitle}");

        return new RawListingDto
        {
            Source = ListingSource.Divar,
            ExternalId = token!,
            Url = $"{BaseUrl}/v/{token}",
            Title = title!,
            RawLocation = GetString(post, "city") ?? GetString(post, "district") ?? slug,
            ModelYear = year,
            MileageKm = mileage,
            PriceToman = price,
            ImageCount = imageCount,
            FirstImageUrl = image,
            BodyCondition = BodyCondition.Unknown,
            // زمان دقیقِ انتشار در کارت لیست همیشه نیست؛ محافظه‌کارانه «همین حالا» تا فیلتر ۲۴ساعت ردش نکند.
            PublishedAt = DateTimeOffset.UtcNow,
            RawJson = post.GetRawText()
        };
    }

    private static long ReadPrice(JsonElement post)
    {
        foreach (string name in new[] { "price", "priceValue", "rawPrice" })
        {
            if (post.TryGetProperty(name, out JsonElement p))
            {
                if (p.ValueKind == JsonValueKind.Number && p.TryGetInt64(out long v))
                {
                    return v;
                }
                if (p.ValueKind == JsonValueKind.String && long.TryParse(DigitsOnly(p.GetString()), out long sv))
                {
                    return sv;
                }
            }
        }
        return 0;
    }

    private static (int? Year, int? Mileage) ParseYearMileage(string text)
    {
        int? year = null, mileage = null;
        Match y = YearRegex().Match(text);
        if (y.Success && int.TryParse(y.Value, out int yv))
        {
            year = yv;
        }
        Match m = MileageRegex().Match(DigitsToLatin(text));
        if (m.Success && int.TryParse(m.Groups[1].Value.Replace(",", string.Empty), out int mv))
        {
            mileage = mv;
        }
        return (year, mileage);
    }

    private static string? GetString(JsonElement e, string name) =>
        e.TryGetProperty(name, out JsonElement v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static string? FirstImageFromArray(JsonElement post)
    {
        if (post.TryGetProperty("images", out JsonElement imgs) && imgs.ValueKind == JsonValueKind.Array && imgs.GetArrayLength() > 0)
        {
            JsonElement first = imgs[0];
            return first.ValueKind == JsonValueKind.String ? first.GetString() : GetString(first, "url") ?? GetString(first, "src");
        }
        return null;
    }

    private static string DigitsOnly(string? s) => new((s ?? string.Empty).Where(char.IsDigit).ToArray());

    private static string DigitsToLatin(string s)
    {
        Span<char> buf = stackalloc char[s.Length];
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            buf[i] = c switch { >= '۰' and <= '۹' => (char)('0' + (c - '۰')), >= '٠' and <= '٩' => (char)('0' + (c - '٠')), _ => c };
        }
        return new string(buf);
    }

    [GeneratedRegex("<script id=\"__NEXT_DATA__\" type=\"application/json\">(.*?)</script>", RegexOptions.Singleline)]
    private static partial Regex NextDataRegex();

    [GeneratedRegex("window\\.__PRELOADED_STATE__\\s*=\\s*(\\{.*?\\});", RegexOptions.Singleline)]
    private static partial Regex PreloadRegex();

    [GeneratedRegex("1[34]\\d{2}")]
    private static partial Regex YearRegex();

    [GeneratedRegex("([\\d,]{3,})\\s*(?:کیلومتر|km|هزار)")]
    private static partial Regex MileageRegex();
}
