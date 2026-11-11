using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shekar.Application.Contracts;
using Shekar.Infrastructure.Crawling;
using Xunit;

namespace Shekar.Infrastructure.Tests;

/// <summary>
/// تستِ رگرسیونِ پارسرِ دیوار روی HTMLِ ذخیره‌شده (fixture). اگر ساختارِ صفحه یا منطقِ پارس
/// بشکند، این تست زودتر از محیطِ واقعی خطا می‌دهد (بدونِ هیچ درخواستِ شبکه).
/// fixtureها هم‌شکلِ خروجیِ واقعیِ دیوار هستند؛ هنگامِ تغییرِ سایت باید از یک کپچرِ تازه نو شوند.
/// </summary>
public class DivarHtmlProviderTests
{
    private static string Fixture(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));

    private static DivarHtmlProvider Build(IWebFetcher fetcher) =>
        new(fetcher, Options.Create(new PoliteFetchOptions()), NullLogger<DivarHtmlProvider>.Instance);

    [Fact]
    public async Task ParsesListPage_IntoListings()
    {
        var fetcher = new FakeFetcher(Fixture("divar-list.html"), Fixture("divar-detail.html"));

        IReadOnlyList<RawListingDto> results =
            await Build(fetcher).FetchRecentAsync(1, new HashSet<string>(), CancellationToken.None);

        Assert.Equal(2, results.Count);

        RawListingDto peugeot = results.Single(r => r.ExternalId == "AbC12345");
        Assert.Equal("پژو ۲۰۶ تیپ ۲ مدل ۱۳۹۶", peugeot.Title);
        Assert.Equal(450_000_000, peugeot.PriceToman);          // از متنِ فارسیِ لیست
        Assert.Equal("پیروزی", peugeot.RawLocation);            // محله از web_info
        Assert.Equal(4, peugeot.ImageCount);
        Assert.Equal("https://divar.ir/v/AbC12345", peugeot.Url);
    }

    [Fact]
    public async Task EnrichesFromDetailPage_WithYearModelMileageColor()
    {
        var fetcher = new FakeFetcher(Fixture("divar-list.html"), Fixture("divar-detail.html"));

        IReadOnlyList<RawListingDto> results =
            await Build(fetcher).FetchRecentAsync(1, new HashSet<string>(), CancellationToken.None);

        RawListingDto peugeot = results.Single(r => r.ExternalId == "AbC12345");
        Assert.Equal(1396, peugeot.ModelYear);                        // از «مدل (سال تولید)»
        Assert.Equal("Peugeot 206 2-normal", peugeot.SourceModelKey); // مدلِ قطعی (DivarKey)
        Assert.Equal(120_000, peugeot.MileageKm);                     // از «کارکرد»
        Assert.Equal("سفید", peugeot.Color);                          // از «رنگ»
        Assert.Equal(450_000_000, peugeot.PriceToman);                // قیمتِ لیست حفظ می‌شود (نه «قیمت پایه»)
    }

    [Fact]
    public void ExtractPreloadedState_IgnoresChallengePage_WithoutAssignment()
    {
        // صفحه‌ی چلنجِ ضدبات اغلب فقط گاردِ typeof دارد، نه انتسابِ واقعی → باید null شود.
        const string challenge = "<html><script>if (typeof window.__PRELOADED_STATE__ === 'undefined') {}</script></html>";
        Assert.Null(DivarHtmlProvider.ExtractPreloadedState(challenge));
    }

    /// <summary>IWebFetcher ساختگی: صفحه‌ی لیست را برای URLهای بدونِ /v/ و جزئیات را برای /v/ می‌دهد.</summary>
    private sealed class FakeFetcher : IWebFetcher
    {
        private readonly string _listHtml;
        private readonly string _detailHtml;

        public FakeFetcher(string listHtml, string detailHtml)
        {
            _listHtml = listHtml;
            _detailHtml = detailHtml;
        }

        public IFetchSession StartSession(string? referer = null) => new Session(_listHtml, _detailHtml);

        private sealed class Session : IFetchSession
        {
            private readonly string _listHtml;
            private readonly string _detailHtml;

            public Session(string listHtml, string detailHtml)
            {
                _listHtml = listHtml;
                _detailHtml = detailHtml;
            }

            public Task<string?> GetHtmlAsync(string url, CancellationToken ct = default)
            {
                if (url.Contains("/v/", StringComparison.Ordinal))
                {
                    return Task.FromResult<string?>(_detailHtml);
                }

                // فقط صفحه‌ی اولِ لیست؛ صفحه‌ی دوم را خالی بده تا پیمایش متوقف شود.
                return Task.FromResult(url.Contains("page=", StringComparison.Ordinal) ? null : _listHtml);
            }

            public void Dispose() { }
        }
    }
}
