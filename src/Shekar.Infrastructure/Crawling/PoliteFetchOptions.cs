namespace Shekar.Infrastructure.Crawling;

/// <summary>
/// تنظیمات رفتارِ انسان‌مانندِ کراول برای جلوگیری از بلاک‌شدن IP/درخواست‌ها.
/// درخواست‌ها سریالی، با تأخیرِ تصادفی (نه ثابت)، با استراحتِ گاه‌به‌گاه و بودجه‌ی محدود در هر پاس.
/// </summary>
public sealed class PoliteFetchOptions
{
    /// <summary>کمینه‌ی تأخیر بین دو درخواست (ثانیه).</summary>
    public double MinDelaySeconds { get; set; } = 5;

    /// <summary>بیشینه‌ی تأخیر بین دو درخواست (ثانیه) — تأخیر واقعی به‌صورت تصادفی در این بازه.</summary>
    public double MaxDelaySeconds { get; set; } = 16;

    /// <summary>هر چند درخواست، یک استراحتِ بلندترِ انسان‌مانند انجام شود.</summary>
    public int LongPauseEveryRequests { get; set; } = 7;

    /// <summary>کمینه‌ی مدت استراحتِ بلند (ثانیه).</summary>
    public double LongPauseMinSeconds { get; set; } = 40;

    /// <summary>بیشینه‌ی مدت استراحتِ بلند (ثانیه).</summary>
    public double LongPauseMaxSeconds { get; set; } = 120;

    /// <summary>بیشینه‌ی تعداد صفحه‌ی لیست در هر پاس (فقط آگهی‌های تازه).</summary>
    public int MaxPagesPerRun { get; set; } = 2;

    /// <summary>بیشینه‌ی کل درخواست در هر پاس (سقف بودجه).</summary>
    public int MaxRequestsPerRun { get; set; } = 6;

    /// <summary>مهلت هر درخواست (ثانیه).</summary>
    public int RequestTimeoutSeconds { get; set; } = 25;

    /// <summary>بیشینه‌ی تلاش مجدد برای خطای گذرا (timeout/5xx).</summary>
    public int MaxRetries { get; set; } = 2;

    /// <summary>فهرست User-Agentهای واقعی مرورگر؛ هر درخواست یکی به‌صورت تصادفی انتخاب می‌شود.</summary>
    public List<string> UserAgents { get; set; } = new()
    {
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/125.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:126.0) Gecko/20100101 Firefox/126.0",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36",
        "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123.0.0.0 Safari/537.36"
    };

    /// <summary>نگاشت شناسه‌ی شهر → اسلاگ دیوار (مثلاً 1→tehran).</summary>
    public Dictionary<int, string> CitySlugs { get; set; } = new() { [1] = "tehran" };

    /// <summary>
    /// پارامترهای فیلتر ثابت (بدون علامت «?» ابتدایی) که به همه‌ی URL های کراول اضافه می‌شود.
    /// مثال: business-type=personal%2C&amp;exchange=exclude-exchanges&amp;has-photo=true&amp;recent_ads=3h&amp;usage=0-200000
    /// </summary>
    public string FilterQuery { get; set; } =
        "business-type=personal%2C&exchange=exclude-exchanges&has-photo=true&recent_ads=3h&usage=0-200000";
}
