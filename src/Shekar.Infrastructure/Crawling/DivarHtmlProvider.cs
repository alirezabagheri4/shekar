using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shekar.Application.Abstractions;
using Shekar.Application.Contracts;
using Shekar.Domain.Enums;

namespace Shekar.Infrastructure.Crawling;

/// <summary>
/// منبع آگهیِ واقعیِ دیوار؛ کراول دومرحله‌ای و مؤدب:
/// (۱) صفحه‌ی لیستِ دسته‌ی خودرو → کشف آگهی‌های تازه (token/عنوان/قیمت/عکس)،
/// (۲) صفحه‌ی جزئیات <c>/v/{token}</c> فقط برای آگهی‌های «جدید» → سال، مدلِ قطعی
/// (<c>webengage.brand_model</c>)، کارکرد و رنگ. آگهیِ تکراری دوباره فچ نمی‌شود، پس
/// حالت پایدار ارزان و کم‌درخواست است. تمام درخواست‌ها از طریق <see cref="IWebFetcher"/>
/// با تأخیر تصادفی، بودجه‌ی محدود و توقف روی ۴۲۹/۴۰۳ انجام می‌شود.
///
/// ساختار داده (تأییدشده روی دیوارِ امروز): HTML خامِ سرور شامل
/// <c>window.__PRELOADED_STATE__ = { ... };</c> است. کارت‌های لیست در گره‌های
/// <c>"@type": ".../widgets.PostRowData"</c> و دادهٔ جزئیات در <c>currentPost.post</c> است.
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

    /// <summary>بدون مجموعه‌ی شناخته‌شده: همه‌ی آگهی‌ها غنی می‌شوند (اجرای دستی/اولین‌بار).</summary>
    public Task<IReadOnlyList<RawListingDto>> FetchRecentAsync(int cityId, CancellationToken ct = default) =>
        FetchRecentAsync(cityId, (IReadOnlySet<string>)new HashSet<string>(StringComparer.Ordinal), ct);

    public async Task<IReadOnlyList<RawListingDto>> FetchRecentAsync(
        int cityId, IReadOnlySet<string> knownExternalIds, CancellationToken ct = default)
    {
        if (!_options.CitySlugs.TryGetValue(cityId, out string? slug))
        {
            _logger.LogWarning("اسلاگ دیوار برای شهر {City} تعریف نشده", cityId);
            return Array.Empty<RawListingDto>();
        }

        var results = new List<RawListingDto>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        using IFetchSession session = _fetcher.StartSession(referer: $"{BaseUrl}/s/{slug}");

        // --- مرحله ۱: صفحه‌ی لیست ---
        for (int page = 0; page < _options.MaxPagesPerRun; page++)
        {
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

        // --- مرحله ۲: صفحه‌ی جزئیات، فقط برای آگهی‌های جدید و در حد بودجه ---
        int enriched = 0, withYear = 0, detailBudget = Math.Max(0, _options.MaxDetailFetchesPerRun);
        for (int i = 0; i < results.Count && enriched < detailBudget; i++)
        {
            RawListingDto summary = results[i];
            if (knownExternalIds.Contains(summary.ExternalId))
            {
                continue; // آگهیِ از قبل ذخیره‌شده؛ جزئیات دوباره فچ نمی‌شود
            }

            string detailUrl = $"{BaseUrl}/v/{summary.ExternalId}";
            string? detailHtml = await session.GetHtmlAsync(detailUrl, ct);
            enriched++;
            if (detailHtml is null)
            {
                break; // بودجه/بلاک؛ بقیه بدون جزئیات می‌مانند (downstream آن‌ها را رد می‌کند)
            }

            RawListingDto before = results[i];
            RawListingDto after = EnrichFromDetail(before, detailHtml);
            results[i] = after;
            if (after.ModelYear is not null)
            {
                withYear++;
            }
            else
            {
                // چلنجِ ضدباتِ دیوار اغلب یک صفحه‌ی کوتاهِ بدون‌مشخصات برمی‌گرداند؛ موردانتظار است و
                // در پاس بعد دوباره تلاش می‌شود (چون بدون‌سال، «غنی‌شده» حساب نمی‌شود).
                _logger.LogDebug(
                    "دیوار: جزئیاتِ {Token} بدون سال (len={Len}) — پاس بعد دوباره تلاش می‌شود",
                    before.ExternalId, detailHtml.Length);
            }
        }

        _logger.LogInformation(
            "دیوار/{Slug}: {Count} آگهی لیست، {Enriched} فچِ جزئیات، {WithYear} سال‌دار",
            slug, results.Count, enriched, withYear);
        return results;
    }

    // ===================== مرحله ۱: پارسِ لیست =====================

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
        string? location = GetWebInfo(post, "district_persian") ?? GetWebInfo(post, "city_persian") ?? slug;

        return new RawListingDto
        {
            Source = ListingSource.Divar,
            ExternalId = token!,
            Url = $"{BaseUrl}/v/{token}",
            Title = title!,
            RawLocation = location,
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

    // ===================== مرحله ۲: پارسِ جزئیات =====================

    /// <summary>
    /// آگهیِ خلاصه را با دادهٔ صفحه‌ی جزئیات غنی می‌کند: سال، مدلِ قطعی، کارکرد، رنگ.
    /// قیمتِ آگهی از لیست حفظ می‌شود (قیمتِ «پایه» در جزئیات، قیمتِ فروش نیست).
    /// </summary>
    private RawListingDto EnrichFromDetail(RawListingDto summary, string detailHtml)
    {
        string? json = ExtractPreloadedState(detailHtml);
        if (json is null)
        {
            return summary;
        }

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(json);
        }
        catch (JsonException)
        {
            return summary;
        }

        using (doc)
        {
            if (!TryGetPath(doc.RootElement, out JsonElement post, "currentPost", "post"))
            {
                return summary;
            }

            // مدلِ قطعی: webengage.brand_model (مثل «Peugeot 206 3-normal»).
            string? sourceModelKey = null;
            if (TryGetPath(post, out JsonElement we, "webengage"))
            {
                sourceModelKey = GetString(we, "brand_model");
            }

            // ردیف‌های مشخصاتِ نمایشی (عنوان=مقدار).
            var specs = new Dictionary<string, string>(StringComparer.Ordinal);
            CollectSpecPairs(post, specs);

            int? year = ParseYearFromSpec(FindSpec(specs, "مدل", "سال"));
            int? mileage = ParseMileage(FindSpec(specs, "کارکرد")) ?? summary.MileageKm;
            string? color = FindSpec(specs, "رنگ");
            string? brandModel = FindSpec(specs, "برند و مدل");
            (string? brand, string? model, string? trim) = SplitBrandModel(brandModel);

            return summary with
            {
                SourceModelKey = string.IsNullOrWhiteSpace(sourceModelKey) ? summary.SourceModelKey : sourceModelKey!.Trim(),
                ModelYear = year ?? summary.ModelYear,
                MileageKm = mileage,
                Color = string.IsNullOrWhiteSpace(color) ? summary.Color : color!.Trim(),
                RawBrand = brand ?? summary.RawBrand,
                RawModel = model ?? summary.RawModel,
                RawTrim = trim ?? summary.RawTrim
            };
        }
    }

    /// <summary>ردیف‌های {title, value} را از درختِ جزئیات جمع می‌کند (آخرین مقدار برای هر عنوان).</summary>
    private static void CollectSpecPairs(JsonElement node, Dictionary<string, string> into)
    {
        switch (node.ValueKind)
        {
            case JsonValueKind.Object:
                string? title = GetString(node, "title");
                string? value = GetString(node, "value");
                if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(value) && value!.Length <= 60)
                {
                    into[title!] = value!;
                }
                foreach (JsonProperty p in node.EnumerateObject())
                {
                    CollectSpecPairs(p.Value, into);
                }
                break;

            case JsonValueKind.Array:
                foreach (JsonElement item in node.EnumerateArray())
                {
                    CollectSpecPairs(item, into);
                }
                break;
        }
    }

    /// <summary>اولین مقداری که عنوانش شاملِ همه‌ی <paramref name="titleParts"/> باشد.</summary>
    private static string? FindSpec(Dictionary<string, string> specs, params string[] titleParts)
    {
        foreach ((string key, string val) in specs)
        {
            if (titleParts.All(part => key.Contains(part, StringComparison.Ordinal)))
            {
                return val;
            }
        }
        return null;
    }

    /// <summary>از «۱۳۸۳ - ۲۰۰۴» یا «۱۳۸۳» سالِ شمسی (۱۳xx/۱۴xx) را درمی‌آورد.</summary>
    private static int? ParseYearFromSpec(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }
        Match y = YearRegex().Match(DigitsToLatin(text));
        return y.Success && int.TryParse(y.Value, out int v) ? v : null;
    }

    /// <summary>«پژو 206 تیپ ۳» → (برند، مدل، تریم). تقریبی برای نمایش؛ هویتِ قطعی DivarKey است.</summary>
    private static (string? Brand, string? Model, string? Trim) SplitBrandModel(string? brandModel)
    {
        if (string.IsNullOrWhiteSpace(brandModel))
        {
            return (null, null, null);
        }
        string[] parts = brandModel.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return (null, null, null);
        }
        string brand = parts[0];
        string? model = parts.Length >= 2 ? parts[1] : null;
        string? trim = parts.Length >= 3 ? string.Join(' ', parts[2..]) : null;
        return (brand, model, trim);
    }

    // ===================== استخراج JSON تعبیه‌شده =====================

    /// <summary>
    /// بلاک <c>__PRELOADED_STATE__ = { ... };</c> را با تطبیقِ آکولادِ متوازن (رشته‌آگاه) استخراج می‌کند.
    /// مطمئن‌تر از regex برای JSON چندصدکیلوبایتی است.
    /// </summary>
    internal static string? ExtractPreloadedState(string html)
    {
        // روی خودِ انتساب لنگر می‌اندازیم (نه هر ظهورِ نام)؛ بعضی صفحات گاردِ
        // «typeof window.__PRELOADED_STATE__» هم دارند و صفحه‌ی بلاک اصلاً انتساب ندارد.
        Match assign = PreloadAssignRegex().Match(html);
        if (!assign.Success)
        {
            return null;
        }

        int start = assign.Index + assign.Length - 1; // موقعیتِ «{»

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

    // ===================== کمکی‌ها =====================

    private static bool TryGetPath(JsonElement root, out JsonElement found, params string[] path)
    {
        JsonElement cur = root;
        foreach (string key in path)
        {
            if (cur.ValueKind != JsonValueKind.Object || !cur.TryGetProperty(key, out cur))
            {
                found = default;
                return false;
            }
        }
        found = cur;
        return true;
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

    /// <summary>کارکرد را از متنِ «۱۴۷,۰۰۰ کیلومتر» یا «۱۷۵۰۰۰» درمی‌آورد؛ «۰…» → ۰. بدون عدد → null.</summary>
    private static int? ParseMileage(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }
        string latin = DigitsToLatin(text);
        Match m = MileageRegex().Match(latin);
        if (m.Success && int.TryParse(m.Groups[1].Value.Replace(",", string.Empty), out int v))
        {
            return v;
        }
        // متنِ جزئیات گاهی فقط عدد است («۱۷۵۰۰۰»).
        string digits = new(latin.Where(char.IsDigit).ToArray());
        return digits.Length is > 0 and <= 7 && int.TryParse(digits, out int d) ? d : null;
    }

    private static string? GetString(JsonElement e, string name) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out JsonElement v) && v.ValueKind == JsonValueKind.String
            ? v.GetString()
            : null;

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

    [GeneratedRegex("__PRELOADED_STATE__\\s*=\\s*\\{", RegexOptions.Singleline)]
    private static partial Regex PreloadAssignRegex();

    [GeneratedRegex("(?<!\\d)1[34]\\d{2}(?!\\d)")]
    private static partial Regex YearRegex();

    [GeneratedRegex("([\\d,]{1,})\\s*(?:کیلومتر|km)")]
    private static partial Regex MileageRegex();
}
