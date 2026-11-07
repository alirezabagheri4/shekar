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
/// تمام درخواست‌ها از طریق <see cref="IWebFetcher"/> با نرخ پایین و مؤدب انجام می‌شود؛
/// فقط چند صفحه‌ی اول (آگهی‌های تازه) خوانده می‌شود و صفحه‌ی جزئیات برای کاهش بار فچ نمی‌شود.
///
/// ساختار داده (تأییدشده روی دیوارِ امروز): HTML خامِ سرور شامل
/// <c>window.__PRELOADED_STATE__ = { ... };</c> است. آگهی‌ها در گره‌هایی با
/// <c>"@type": ".../widgets.PostRowData"</c> قرار دارند و فیلدهای نمایشی دارند:
/// title، token، image_url/image_count، middle_description_text (قیمت)،
/// top_description_text (کارکرد)، action.payload.web_info (شهر/محله).
/// </summary>
public sealed partial class DivarHtmlProvider : IListingSourceProvider
{
    private const string BaseUrl = "https://divar.ir";
    private const string PostRowTypeSuffix = "PostRowData";

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
                break; // صفحه‌ی خالی، تکراری یا الگوی تغییریافته → ادامه نده
            }
        }

        _logger.LogInformation("دیوار/{Slug}: {Count} آگهی استخراج شد", slug, results.Count);
        return results;
    }

    /// <summary>از HTML، JSON تعبیه‌شده را درمی‌آورد و گره‌های آگهی (PostRowData) را نگاشت می‌کند.</summary>
    private IEnumerable<RawListingDto> ParsePage(string html, string slug)
    {
        string? json = ExtractPreloadedState(html);
        if (json is null)
        {
            _logger.LogWarning("دیوار/{Slug}: بلاک __PRELOADED_STATE__ در HTML پیدا نشد (ساختار صفحه تغییر کرده؟)", slug);
            yield break;
        }

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "دیوار/{Slug}: پارس JSON صفحه ناموفق بود", slug);
            yield break;
        }

        using (doc)
        {
            foreach (JsonElement post in FindPostRows(doc.RootElement))
            {
                RawListingDto? dto = MapPost(post, slug);
                if (dto is not null)
                {
                    yield return dto;
                }
            }
        }
    }

    /// <summary>
    /// بلاک <c>__PRELOADED_STATE__ = { ... };</c> را با تطبیقِ آکولادِ متوازن (رشته‌آگاه) استخراج می‌کند.
    /// مطمئن‌تر از regex برای JSON چندصدکیلوبایتی است.
    /// </summary>
    internal static string? ExtractPreloadedState(string html)
    {
        int marker = html.IndexOf("__PRELOADED_STATE__", StringComparison.Ordinal);
        if (marker < 0)
        {
            return null;
        }

        int eq = html.IndexOf('=', marker);
        if (eq < 0)
        {
            return null;
        }

        int start = html.IndexOf('{', eq);
        if (start < 0)
        {
            return null;
        }

        int depth = 0;
        bool inString = false, escape = false;
        for (int i = start; i < html.Length; i++)
        {
            char c = html[i];
            if (inString)
            {
                if (escape) { escape = false; }
                else if (c == '\\') { escape = true; }
                else if (c == '"') { inString = false; }
                continue;
            }

            switch (c)
            {
                case '"': inString = true; break;
                case '{': depth++; break;
                case '}':
                    depth--;
                    if (depth == 0)
                    {
                        return html.Substring(start, i - start + 1);
                    }
                    break;
            }
        }

        return null; // آکولادِ بسته‌ی متناظر پیدا نشد (HTML ناقص)
    }

    /// <summary>به‌صورت بازگشتی هر آبجکتِ آگهی (<c>@type</c> منتهی به PostRowData) را می‌یابد.</summary>
    private static IEnumerable<JsonElement> FindPostRows(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                if (IsPostRow(element))
                {
                    yield return element;
                    yield break; // داخل یک آگهی دنبال آگهیِ تودرتو نگرد
                }
                foreach (JsonProperty prop in element.EnumerateObject())
                {
                    foreach (JsonElement found in FindPostRows(prop.Value))
                    {
                        yield return found;
                    }
                }
                break;

            case JsonValueKind.Array:
                foreach (JsonElement item in element.EnumerateArray())
                {
                    foreach (JsonElement found in FindPostRows(item))
                    {
                        yield return found;
                    }
                }
                break;
        }
    }

    private static bool IsPostRow(JsonElement obj)
    {
        if (obj.TryGetProperty("@type", out JsonElement t)
            && t.ValueKind == JsonValueKind.String
            && (t.GetString()?.EndsWith(PostRowTypeSuffix, StringComparison.Ordinal) ?? false))
        {
            return true;
        }

        // پشتیبان: اگر @type نبود، آبجکتی که هم token و هم متنِ قیمت دارد را آگهی بشمار.
        return obj.TryGetProperty("token", out _) && obj.TryGetProperty("middle_description_text", out _);
    }

    private RawListingDto? MapPost(JsonElement post, string slug)
    {
        string? token = GetString(post, "token") ?? GetTokenFromAction(post);
        string? title = GetString(post, "title");
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(title))
        {
            return null;
        }

        long price = ParseMoney(GetString(post, "middle_description_text"));
        int? mileage = ParseMileage(GetString(post, "top_description_text"));
        string? image = GetString(post, "image_url");
        int imageCount = GetInt(post, "image_count") ?? (image is null ? 0 : 1);
        (int? year, _) = ParseYearMileage(title!);
        string? location = GetWebInfo(post, "district_persian") ?? GetWebInfo(post, "city_persian") ?? slug;

        return new RawListingDto
        {
            Source = ListingSource.Divar,
            ExternalId = token!,
            Url = $"{BaseUrl}/v/{token}",
            Title = title!,
            RawLocation = location,
            ModelYear = year,
            MileageKm = mileage,
            PriceToman = price,
            ImageCount = imageCount,
            FirstImageUrl = image,
            BodyCondition = BodyCondition.Unknown,
            // زمان دقیقِ انتشار در کارت لیست نیست؛ چون با recent_ads فیلتر می‌کنیم، محافظه‌کارانه «همین حالا».
            PublishedAt = DateTimeOffset.UtcNow,
            RawJson = post.GetRawText()
        };
    }

    private static string? GetTokenFromAction(JsonElement post) =>
        post.TryGetProperty("action", out JsonElement a) && a.ValueKind == JsonValueKind.Object
        && a.TryGetProperty("payload", out JsonElement p) && p.ValueKind == JsonValueKind.Object
            ? GetString(p, "token")
            : null;

    private static string? GetWebInfo(JsonElement post, string name) =>
        post.TryGetProperty("action", out JsonElement a) && a.ValueKind == JsonValueKind.Object
        && a.TryGetProperty("payload", out JsonElement p) && p.ValueKind == JsonValueKind.Object
        && p.TryGetProperty("web_info", out JsonElement w) && w.ValueKind == JsonValueKind.Object
            ? GetString(w, name)
            : null;

    /// <summary>متنِ قیمتِ فارسی («۱,۴۵۰,۰۰۰,۰۰۰ تومان») را به عدد تبدیل می‌کند؛ «توافقی»/بدون‌رقم → ۰.</summary>
    private static long ParseMoney(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }
        string digits = new(DigitsToLatin(text).Where(char.IsDigit).ToArray());
        return long.TryParse(digits, out long v) ? v : 0;
    }

    /// <summary>کارکرد را از متنِ «۱۴۷,۰۰۰ کیلومتر» درمی‌آورد؛ «۰ کیلومتر» → ۰. بدون عدد → null.</summary>
    private static int? ParseMileage(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }
        Match m = MileageRegex().Match(DigitsToLatin(text));
        return m.Success && int.TryParse(m.Groups[1].Value.Replace(",", string.Empty), out int v) ? v : null;
    }

    private static (int? Year, int? Mileage) ParseYearMileage(string text)
    {
        int? year = null, mileage = null;
        Match y = YearRegex().Match(DigitsToLatin(text));
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

    private static int? GetInt(JsonElement e, string name) =>
        e.TryGetProperty(name, out JsonElement v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out int i) ? i : null;

    private static string DigitsToLatin(string s)
    {
        Span<char> buf = stackalloc char[s.Length];
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            buf[i] = c switch
            {
                >= '۰' and <= '۹' => (char)('0' + (c - '۰')),
                >= '٠' and <= '٩' => (char)('0' + (c - '٠')),
                _ => c
            };
        }
        return new string(buf);
    }

    [GeneratedRegex("(?<!\\d)1[34]\\d{2}(?!\\d)")]
    private static partial Regex YearRegex();

    [GeneratedRegex("([\\d,]{1,})\\s*(?:کیلومتر|km)")]
    private static partial Regex MileageRegex();
}
