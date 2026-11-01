using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Shekar.Infrastructure.Crawling;

/// <summary>یک «نشست» کراول با بودجه و پیمایشِ انسان‌مانند؛ در هر پاس کراول یک نشست ساخته می‌شود.</summary>
public interface IFetchSession : IDisposable
{
    /// <summary>HTML یک آدرس را مؤدبانه می‌گیرد؛ <c>null</c> اگر بودجه تمام شد، بلاک شدیم یا خطا خورد.</summary>
    Task<string?> GetHtmlAsync(string url, CancellationToken ct = default);
}

/// <summary>سازنده‌ی نشستِ کراولِ مؤدب.</summary>
public interface IWebFetcher
{
    IFetchSession StartSession(string? referer = null);
}

public sealed class PoliteWebFetcher : IWebFetcher
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly PoliteFetchOptions _options;
    private readonly ILogger<PoliteWebFetcher> _logger;

    public PoliteWebFetcher(IHttpClientFactory httpFactory, IOptions<PoliteFetchOptions> options, ILogger<PoliteWebFetcher> logger)
    {
        _httpFactory = httpFactory;
        _options = options.Value;
        _logger = logger;
    }

    public IFetchSession StartSession(string? referer = null) =>
        new Session(_httpFactory.CreateClient("crawler"), _options, _logger, referer);

    private sealed class Session : IFetchSession
    {
        private readonly HttpClient _http;
        private readonly PoliteFetchOptions _o;
        private readonly ILogger _logger;
        private readonly Random _rnd = Random.Shared;
        private string? _referer;
        private int _requestCount;
        private bool _blocked;

        public Session(HttpClient http, PoliteFetchOptions o, ILogger logger, string? referer)
        {
            _http = http;
            _o = o;
            _logger = logger;
            _referer = referer;
            _http.Timeout = TimeSpan.FromSeconds(o.RequestTimeoutSeconds);
        }

        public async Task<string?> GetHtmlAsync(string url, CancellationToken ct = default)
        {
            if (_blocked)
            {
                return null; // پس از بلاک، بقیه‌ی درخواست‌های این پاس را نمی‌زنیم
            }
            if (_requestCount >= _o.MaxRequestsPerRun)
            {
                _logger.LogInformation("بودجه‌ی درخواستِ این پاس تمام شد ({Max})", _o.MaxRequestsPerRun);
                return null;
            }

            await HumanDelayAsync(ct);

            for (int attempt = 0; attempt <= _o.MaxRetries; attempt++)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    using var req = BuildRequest(url);
                    _requestCount++;
                    using HttpResponseMessage res = await _http.SendAsync(req, HttpCompletionOption.ResponseContentRead, ct);

                    if (res.StatusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.Forbidden)
                    {
                        // علامتِ بلاک: متوقف شو و این منبع را تا پاس بعد رها کن.
                        _blocked = true;
                        _logger.LogWarning("پاسخ {Code} از منبع؛ توقف این پاس برای جلوگیری از بلاک شدن", (int)res.StatusCode);
                        return null;
                    }

                    if ((int)res.StatusCode >= 500)
                    {
                        await BackoffAsync(attempt, ct);
                        continue; // تلاش مجدد برای خطای سرور
                    }

                    res.EnsureSuccessStatusCode();
                    string body = await res.Content.ReadAsStringAsync(ct);
                    _referer = url; // رفتار طبیعی: Referer زنجیره‌ای
                    return body;
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "خطای درخواست (تلاش {Attempt})", attempt + 1);
                    if (attempt >= _o.MaxRetries)
                    {
                        return null;
                    }
                    await BackoffAsync(attempt, ct);
                }
            }

            return null;
        }

        private HttpRequestMessage BuildRequest(string url)
        {
            var req = new HttpRequestMessage(HttpMethod.Get, url);
            string ua = _o.UserAgents.Count > 0 ? _o.UserAgents[_rnd.Next(_o.UserAgents.Count)] : "Mozilla/5.0";
            req.Headers.TryAddWithoutValidation("User-Agent", ua);
            req.Headers.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8");
            req.Headers.TryAddWithoutValidation("Accept-Language", "fa-IR,fa;q=0.9,en;q=0.6");
            req.Headers.TryAddWithoutValidation("Upgrade-Insecure-Requests", "1");
            req.Headers.TryAddWithoutValidation("Sec-Fetch-Dest", "document");
            req.Headers.TryAddWithoutValidation("Sec-Fetch-Mode", "navigate");
            req.Headers.TryAddWithoutValidation("Sec-Fetch-Site", _referer is null ? "none" : "same-origin");
            if (ua.Contains("Chrome"))
            {
                req.Headers.TryAddWithoutValidation("sec-ch-ua", "\"Chromium\";v=\"125\", \"Not.A/Brand\";v=\"24\"");
                req.Headers.TryAddWithoutValidation("sec-ch-ua-mobile", "?0");
                req.Headers.TryAddWithoutValidation("sec-ch-ua-platform", "\"Windows\"");
            }
            if (_referer is not null)
            {
                req.Headers.TryAddWithoutValidation("Referer", _referer);
            }
            return req;
        }

        /// <summary>تأخیرِ تصادفیِ انسان‌مانند پیش از هر درخواست + استراحتِ بلندِ گاه‌به‌گاه.</summary>
        private async Task HumanDelayAsync(CancellationToken ct)
        {
            double seconds = _o.MinDelaySeconds + _rnd.NextDouble() * Math.Max(0, _o.MaxDelaySeconds - _o.MinDelaySeconds);
            if (_requestCount > 0 && _o.LongPauseEveryRequests > 0 && _requestCount % _o.LongPauseEveryRequests == 0)
            {
                seconds = _o.LongPauseMinSeconds + _rnd.NextDouble() * Math.Max(0, _o.LongPauseMaxSeconds - _o.LongPauseMinSeconds);
            }
            await Task.Delay(TimeSpan.FromSeconds(seconds), ct);
        }

        private async Task BackoffAsync(int attempt, CancellationToken ct)
        {
            double baseSeconds = Math.Pow(2, attempt + 1);        // 2,4,8…
            double jitter = _rnd.NextDouble() * baseSeconds;       // + تصادفی
            await Task.Delay(TimeSpan.FromSeconds(baseSeconds + jitter), ct);
        }

        public void Dispose() { }
    }
}
