using Shekar.Domain;
using Shekar.Domain.Entities;
using Shekar.Domain.Enums;
using Shekar.Application.Services;
using Xunit;

namespace Shekar.Application.Tests;

/// <summary>
/// تست‌های مستقیمِ «مغزِ امتیازدهی» (فازِ S۱): سیگنالِ قیمتِ نسبت‌به‌پراکندگی (G1)،
/// اطمینانِ مبتنی‌بر‌اعتبار (G2) و گیتِ اعتبار روی طلایی (G4).
/// </summary>
public class HuntScorerTests
{
    private readonly HuntScorer _scorer = new();

    private static CarListing Listing(long price) => new()
    {
        Id = 1, Source = ListingSource.Divar, ExternalId = "x", Title = "x",
        VehicleModelId = 1, ModelYear = 1402, BodyCondition = BodyCondition.Healthy,
        PriceToman = price, CityId = 1, State = ListingState.Active
    };

    private static VehicleModel Model(int liquidity = 70) => new()
    {
        Id = 1, Brand = "پژو", Model = "۲۰۷", LiquidityScore = liquidity
    };

    private static MarketSegmentStat Segment(
        long median, int samples, int spreadPercent = 0, byte level = 0, bool reliable = false, int trend = 0) => new()
    {
        SegmentKey = "k", VehicleModelId = 1, ModelYear = 1402,
        MileageBucket = MileageBucket.To20k, BodyCondition = BodyCondition.Healthy,
        SampleCount = samples, MedianPriceToman = median, SpreadPercent = spreadPercent,
        Level = level, IsReliable = reliable, TrendPercent = trend
    };

    private static ModelLiquidityStat Liq(int medianDomDays, int removalRate, int removedCount) => new()
    {
        VehicleModelId = 1, MedianDomDays = medianDomDays, RemovalRatePercent = removalRate,
        RemovedCount = removedCount, SampleCount = removedCount, IsReliable = removedCount >= 5
    };

    [Fact]
    public void G1_TightMarket_ScoresHigherThanWideMarket_ForSameBelowPercent()
    {
        // همان ۱۰٪ زیر بازار؛ ولی در بازارِ فشرده (پراکندگی ۱۰٪) باید امتیازِ قیمتِ بیشتری از بازارِ پراکنده (۴۰٪) بگیرد.
        var listing = Listing(900_000_000);
        var model = Model();
        var o = new ScoringOptions();

        int tight = _scorer.Score(listing, Segment(1_000_000_000, samples: 8, spreadPercent: 10, reliable: true), model, null, o).Score;
        int wide = _scorer.Score(listing, Segment(1_000_000_000, samples: 8, spreadPercent: 40, reliable: true), model, null, o).Score;

        Assert.True(tight > wide, $"بازارِ فشرده باید امتیازِ بالاتری بگیرد (فشرده={tight} پراکنده={wide})");
    }

    [Fact]
    public void G2_CoarserBackoffLevel_LowersConfidence_AndScore()
    {
        // دو مرجعِ یکسان جز سطحِ backoff: سطحِ درشت‌تر (L3) باید اطمینان و در نتیجه امتیازِ کمتری بدهد.
        var listing = Listing(900_000_000);
        var model = Model();
        var o = new ScoringOptions();

        int exact = _scorer.Score(listing, Segment(1_000_000_000, samples: 8, spreadPercent: 12, level: 0, reliable: true), model, null, o).Score;
        int coarse = _scorer.Score(listing, Segment(1_000_000_000, samples: 8, spreadPercent: 12, level: 3, reliable: true), model, null, o).Score;

        Assert.True(exact > coarse, $"سطحِ دقیق باید امتیازِ بالاتری بگیرد (دقیق={exact} درشت={coarse})");
    }

    [Fact]
    public void G4_Golden_RequiresReliableReference()
    {
        // مرجعِ غیرقابل‌اتکا نباید طلایی شود، حتی اگر امتیاز بالا باشد؛ همان مرجع اگر قابل‌اتکا شود، طلایی می‌شود.
        var listing = Listing(820_000_000); // ~۱۸٪ زیر بازار
        var model = Model(liquidity: 95);
        var o = new ScoringOptions();

        var unreliable = _scorer.Score(listing, Segment(1_000_000_000, samples: 8, spreadPercent: 10, reliable: false), model, null, o);
        var reliable = _scorer.Score(listing, Segment(1_000_000_000, samples: 8, spreadPercent: 10, reliable: true), model, null, o);

        Assert.True(unreliable.Score >= DomainConstants.GoldenScoreThreshold, "پیش‌شرطِ تست: امتیاز باید به آستانه‌ی طلایی برسد.");
        Assert.False(unreliable.IsGolden); // گیتِ اعتبار
        Assert.True(reliable.IsGolden);
    }

    [Fact]
    public void G3_DataLiquidity_FastSellingModel_ScoresHigher_AndIsHighBand()
    {
        // دو مدل با نقدشوندگیِ کاتالوگِ یکسانِ پایین؛ مدلی که سریع می‌فروشد (DOMِ کم، نرخِ حذفِ بالا، شواهدِ کافی)
        // باید liqCompِ بالاتر و برچسبِ «نقدشوندگیِ بالا» بگیرد، در حالی‌که مدلِ کُندفروش پایین می‌ماند.
        var listing = Listing(900_000_000);
        var model = Model(liquidity: 40); // کاتالوگ: متوسط
        var seg = Segment(1_000_000_000, samples: 8, spreadPercent: 10, reliable: true);
        var o = new ScoringOptions();

        var fast = _scorer.Score(listing, seg, model, Liq(medianDomDays: 5, removalRate: 80, removedCount: 20), o);
        var slow = _scorer.Score(listing, seg, model, Liq(medianDomDays: 90, removalRate: 5, removedCount: 20), o);

        Assert.True(fast.Score > slow.Score, $"مدلِ سریع‌فروش باید امتیازِ بالاتری بگیرد (سریع={fast.Score} کُند={slow.Score})");
        Assert.Equal(Liquidity.High, fast.Liquidity);
        Assert.Equal(Liquidity.Medium, slow.Liquidity);
    }

    [Fact]
    public void G3_DataLiquidity_ColdStart_ShrinksTowardCatalog()
    {
        // نقدشوندگیِ داده‌محورِ قوی (DOMِ کم)، ولی با حذفِ کم باید عمدتاً به کاتالوگ بچسبد؛ هرچه حذف بیشتر،
        // بیشتر به‌سمتِ داده کشیده شود. پس فاصله‌ی «کم‌حذف» از کاتالوگ باید کمتر از فاصله‌ی «پرحذف» باشد.
        var listing = Listing(900_000_000);
        var model = Model(liquidity: 40);
        var seg = Segment(1_000_000_000, samples: 8, spreadPercent: 10, reliable: true);
        var o = new ScoringOptions();

        int catalog = _scorer.Score(listing, seg, model, null, o).Score;
        int few = _scorer.Score(listing, seg, model, Liq(medianDomDays: 5, removalRate: 80, removedCount: 1), o).Score;
        int many = _scorer.Score(listing, seg, model, Liq(medianDomDays: 5, removalRate: 80, removedCount: 20), o).Score;

        Assert.True(catalog < few && few < many, $"shrinkage باید تدریجی باشد (کاتالوگ={catalog} کم‌حذف={few} پرحذف={many})");
        Assert.True(few - catalog < many - few, "کم‌حذف باید نزدیکِ کاتالوگ بماند (cold-start)");
    }
}
