using Shekar.Domain;
using Shekar.Domain.Entities;
using Shekar.Domain.Enums;

namespace Shekar.Application.Services;

/// <summary>پارامترهای قابل‌تنظیم اسکورینگ (قوانین از کد بیرون کشیده شده‌اند).</summary>
public sealed record ScoringOptions
{
    /// <summary>حداقل درصد زیر بازار برای واجد شرایط شدن.</summary>
    public int MinBelowMarketPercent { get; init; } = 5;

    /// <summary>حداقل امتیاز برای ورود به تاپ‌ها.</summary>
    public int MinScore { get; init; } = 60;

    /// <summary>حداقل تعداد نمونه‌ی سگمنت برای اعتماد به میانه.</summary>
    public int MinSamples { get; init; } = 3;

    /// <summary>
    /// درصد زیر بازاری که مؤلفه‌ی قیمت را اشباع می‌کند — فقط زمانی که پراکندگیِ سگمنت نامعلوم است
    /// (رفتارِ پیشین). وقتی سگمنت پراکندگی دارد، اشباع از روی پراکندگی محاسبه می‌شود (نگاه کنید به
    /// <see cref="SpreadSaturationMultiple"/>) تا «زیر بازار» از نظر آماری بین سگمنت‌ها قابل‌مقایسه شود.
    /// </summary>
    public int BelowMarketSaturation { get; init; } = 20;

    /// <summary>
    /// سقفِ منطقیِ «زیر بازار»؛ بالاتر از آن آگهی مشکوک است (کلاه‌برداری/اقساطی/قیمتِ تایپی)
    /// و نباید به‌عنوان فرصت (به‌ویژه طلایی) نمایش داده شود. به‌جای آن «مشکوک» علامت می‌خورد.
    /// </summary>
    public int SuspiciousBelowMarketPercent { get; init; } = 40;

    /// <summary>تعداد نمونه‌ای که «کفایتِ نمونه»ی مؤلفه‌ی اطمینان را کامل می‌کند (۵ نمونه → کامل).</summary>
    public int ConfidenceFullAtSamples { get; init; } = 5;

    /// <summary>روندِ صعودیِ قابل‌توجه (٪)؛ بالاتر از آن، بازار رو به رشد است و احتیاطِ بیشتری لازم است.</summary>
    public int RisingMarketTrendPercent { get; init; } = 5;

    /// <summary>
    /// حاشیه‌ی زیربازاریِ اضافیِ لازم در بازارِ صعودی. در بازارِ رو به رشد، آگهیِ کمی‌زیرِبازار ممکن است
    /// فقط یک آگهیِ عقب‌مانده باشد؛ پس برای واجد شرایط شدن، زیربازاریِ بیشتری می‌طلبیم.
    /// </summary>
    public int RisingMarketExtraMarginPercent { get; init; } = 3;

    // ── سیگنالِ قیمتِ نسبت‌به‌پراکندگی (G1) ───────────────────────────────────────────
    // «زیر بازار»ِ خام از نظر آماری صادق نیست: ۸٪ زیرِ یک بازارِ پراکنده عادی است، ولی همان ۸٪ زیرِ
    // یک بازارِ فشرده سیگنالِ قوی است. پس اشباعِ مؤلفه‌ی قیمت را با پراکندگیِ خودِ سگمنت مقیاس می‌کنیم.

    /// <summary>
    /// ضریبِ تبدیلِ پراکندگی به اشباع: تخفیفی برابرِ (این ضریب × پراکندگیِ نسبیِ سگمنت) مؤلفه‌ی قیمت را
    /// کامل می‌کند. ۱٫۰ یعنی تخفیفی به اندازه‌ی کلِ گستره‌ی میان‌چارکیِ بازار، امتیازِ قیمتِ کامل می‌دهد.
    /// </summary>
    public double SpreadSaturationMultiple { get; init; } = 1.0;

    /// <summary>کفِ اشباعِ قیمت (٪)؛ در بازارِ خیلی فشرده نگذاریم تخفیفِ ناچیز بیش‌ازحد امتیاز بگیرد.</summary>
    public int MinPriceSaturationPercent { get; init; } = 8;

    /// <summary>سقفِ اشباعِ قیمت (٪)؛ در بازارِ خیلی پراکنده اشباع را محدود نگه می‌داریم.</summary>
    public int MaxPriceSaturationPercent { get; init; } = 35;

    // ── اطمینانِ مبتنی‌بر‌اعتبار (G2) ─────────────────────────────────────────────────
    // اطمینان دیگر فقط «تعدادِ نمونه» نیست: فشردگیِ پراکندگی و سطحِ backoff (دقیق در برابر درشت) هم
    // در آن دخیل‌اند؛ شکارِ روی مرجعِ نازک/پراکنده/درشت خودبه‌خود اطمینانِ کمتری می‌گیرد.

    /// <summary>پراکندگیِ نسبی (٪) که در آن یا پایین‌تر، عاملِ پراکندگیِ اطمینان کامل است.</summary>
    public int IdealSpreadPercent { get; init; } = 15;

    /// <summary>بازه‌ی پراکندگی (٪) بالاتر از حدِ ایده‌آل که در طولِ آن عاملِ پراکندگی تا کفِ خود افت می‌کند.</summary>
    public int DispersionSpanPercent { get; init; } = 45;

    /// <summary>کفِ عاملِ پراکندگیِ اطمینان (حتی بازارِ خیلی پراکنده کمی اطمینان دارد).</summary>
    public double DispersionFloor { get; init; } = 0.30;

    /// <summary>
    /// جریمه‌ی اطمینان به‌ازای هر سطحِ درشت‌ترِ backoff: اطمینان × (این عدد)^Level.
    /// L0 (دقیق) جریمه ندارد؛ هر سطحِ درشت‌تر اطمینان را کم می‌کند.
    /// </summary>
    public double LevelPenaltyFactor { get; init; } = 0.85;

    // ── گیتِ اعتبار روی طلایی (G4) ────────────────────────────────────────────────────

    /// <summary>طلایی‌شدن علاوه بر امتیاز، نیازمندِ مرجعِ «قابل‌اتکا» باشد (مرجعِ لرزان طلایی نشود).</summary>
    public bool RequireReliableForGolden { get; init; } = true;

    // ── نقدشوندگیِ داده‌محور + بلندِ کاتالوگ (G3/L۲) ───────────────────────────────────
    // نقدشوندگی دیگر فقط عددِ ثابتِ کاتالوگ نیست: از سرعتِ واقعیِ ترک‌کردنِ بازار (روزهای‌روی‌بازار و
    // نرخِ حذف) ساخته و به‌تناسبِ شواهد به‌سمتِ کاتالوگ shrink می‌شود (پرهیز از پرتگاهِ cold-start).

    /// <summary>روزهای‌روی‌بازاری که در آن یا کمتر، نقدشوندگیِ داده‌محور کامل است (۱۰۰).</summary>
    public int FastDomDays { get; init; } = 7;

    /// <summary>روزهای‌روی‌بازاری که در آن یا بیشتر، نقدشوندگیِ داده‌محور صفر است.</summary>
    public int SlowDomDays { get; init; } = 60;

    /// <summary>نرخِ حذفی (٪) که مؤلفه‌ی نرخِ نقدشوندگی را اشباع می‌کند.</summary>
    public int RateSaturationPercent { get; init; } = 50;

    /// <summary>وزنِ روزهای‌روی‌بازار در نقدشوندگیِ داده‌محور (سیگنالِ قوی‌تر).</summary>
    public double LiquidityDomWeight { get; init; } = 0.7;

    /// <summary>وزنِ نرخِ حذف در نقدشوندگیِ داده‌محور.</summary>
    public double LiquidityRateWeight { get; init; } = 0.3;

    /// <summary>تعداد حذفی که وزنِ شواهد را کامل می‌کند؛ کمتر از آن، نقدشوندگی به‌سمتِ کاتالوگ shrink می‌شود.</summary>
    public int LiquidityEvidenceFullAtRemovals { get; init; } = 10;

    /// <summary>آستانه‌ی نقدشوندگیِ مؤثر برای برچسبِ «نقدشوندگیِ بالا».</summary>
    public int HighLiquidityThreshold { get; init; } = 75;

    public double PriceWeight { get; init; } = 0.55;
    public double LiquidityWeight { get; init; } = 0.35;
    public double ConfidenceWeight { get; init; } = 0.10;
}

/// <summary>نتیجه‌ی اسکورِ یک آگهی نسبت به سگمنتش.</summary>
public sealed record ScoreResult(
    bool Qualifies, int Score, int BelowMarketPercent, long EstimatedProfitToman, Liquidity Liquidity,
    bool IsGolden, bool IsSuspicious, long ExpectedPriceToman);

/// <summary>اسکورر شکار (آماری/هیوریستیک)؛ پشت interface تا بعداً با مدل ML جایگزین شود.</summary>
public interface IHuntScorer
{
    ScoreResult Score(
        CarListing listing, MarketSegmentStat segment, VehicleModel model,
        ModelLiquidityStat? liquidity, ScoringOptions options);
}

public sealed class HuntScorer : IHuntScorer
{
    public ScoreResult Score(
        CarListing listing, MarketSegmentStat segment, VehicleModel model,
        ModelLiquidityStat? liquidity, ScoringOptions o)
    {
        // قیمتِ موردانتظار برای همین کارکرد (میانه + شیب × اختلافِ کارکرد)؛ مرجعِ دقیق‌ترِ «زیر بازار».
        long expected = ExpectedPrice(listing, segment);
        int belowPct = expected > 0 ? (int)Math.Round((expected - listing.PriceToman) * 100.0 / expected) : 0;

        long profit = HuntCalculations.EstimatedNetProfit(expected, listing.PriceToman);

        // مؤلفه‌ها (۰..۱۰۰):
        // قیمت (G1): تخفیف نسبت به پراکندگیِ خودِ سگمنت سنجیده می‌شود؛ بازارِ فشرده‌تر → امتیازِ بیشتر.
        double saturation = PriceSaturation(segment, o);
        double priceComp = Math.Clamp(belowPct * 100.0 / saturation, 0, 100);
        // نقدشوندگی (G3/L۲): داده‌محور (DOM/نرخِ حذف) بلندشده با کاتالوگ؛ در نبودِ آمار، همان کاتالوگ.
        double effectiveLiquidity = EffectiveLiquidity(model, liquidity, o);
        double liqComp = Math.Clamp(effectiveLiquidity, 0, 100);
        // اطمینان (G2): کفایتِ نمونه × فشردگیِ پراکندگی × سطحِ backoff.
        double confComp = Confidence(segment, o) * 100.0;

        int score = (int)Math.Round(o.PriceWeight * priceComp + o.LiquidityWeight * liqComp + o.ConfidenceWeight * confComp);
        score = Math.Clamp(score, 0, 100);

        Liquidity liquidityBand = effectiveLiquidity >= o.HighLiquidityThreshold ? Liquidity.High : Liquidity.Medium;

        // محافظِ کلاه‌برداری/پرت: زیربازارِ بیش از سقفِ منطقی، آگهیِ «مشکوک» است؛ طلایی نمی‌شود
        // و به‌عنوان فرصت واجد شرایط نیست (کاربر را به سمت کلاه‌برداری نمی‌بریم).
        bool suspicious = belowPct >= o.SuspiciousBelowMarketPercent;

        // طلایی (G4): علاوه بر امتیاز و نبودِ شک، مرجع باید قابل‌اتکا باشد تا فرصتِ برتر روی مرجعِ لرزان ننشیند.
        bool golden = score >= DomainConstants.GoldenScoreThreshold
            && !suspicious
            && (!o.RequireReliableForGolden || segment.IsReliable);

        // در بازارِ صعودی، حاشیه‌ی زیربازاریِ بیشتری می‌طلبیم (احتیاط در برابر آگهیِ عقب‌مانده).
        int extraMargin = segment.TrendPercent >= o.RisingMarketTrendPercent ? o.RisingMarketExtraMarginPercent : 0;

        bool qualifies = !suspicious
            && belowPct >= o.MinBelowMarketPercent + extraMargin
            && segment.SampleCount >= o.MinSamples
            && score >= o.MinScore;

        return new ScoreResult(qualifies, score, belowPct, profit, liquidityBand, golden, suspicious, expected);
    }

    /// <summary>
    /// نقدشوندگیِ مؤثرِ ۰..۱۰۰: نقدشوندگیِ داده‌محور (DOM/نرخِ حذف) به‌تناسبِ شواهد (تعدادِ حذف) با
    /// نقدشوندگیِ ثابتِ کاتالوگ بلند می‌شود. بدونِ آمار یا بدونِ حذف → همان کاتالوگ (بی‌پرتگاهِ cold-start).
    /// </summary>
    private static double EffectiveLiquidity(VehicleModel model, ModelLiquidityStat? liq, ScoringOptions o)
    {
        double catalog = Math.Clamp(model.LiquidityScore, 0, 100);
        if (liq is null)
        {
            return catalog;
        }

        double data = DataLiquidityScore(liq, o);
        double w = Math.Clamp(liq.RemovedCount / (double)Math.Max(1, o.LiquidityEvidenceFullAtRemovals), 0, 1);
        return w * data + (1 - w) * catalog;
    }

    /// <summary>
    /// نقدشوندگیِ داده‌محورِ خام ۰..۱۰۰: ترکیبِ وزنیِ «روزهای‌روی‌بازار» (سریع‌تر → بالاتر) و
    /// «نرخِ حذف» (بیشتر → بالاتر). روزهای‌روی‌بازار سیگنالِ قوی‌تر است.
    /// </summary>
    private static double DataLiquidityScore(ModelLiquidityStat liq, ScoringOptions o)
    {
        double span = Math.Max(1, o.SlowDomDays - o.FastDomDays);
        double domScore = Math.Clamp(100.0 * (o.SlowDomDays - liq.MedianDomDays) / span, 0, 100);
        double rateScore = Math.Clamp(liq.RemovalRatePercent * 100.0 / Math.Max(1, o.RateSaturationPercent), 0, 100);

        double total = o.LiquidityDomWeight + o.LiquidityRateWeight;
        return total > 0 ? (o.LiquidityDomWeight * domScore + o.LiquidityRateWeight * rateScore) / total : domScore;
    }

    /// <summary>
    /// اشباعِ مؤلفه‌ی قیمت (٪): از روی پراکندگیِ نسبیِ سگمنت. بازارِ فشرده‌تر با همان تخفیف امتیازِ بیشتری
    /// می‌گیرد. اگر پراکندگی نامعلوم باشد (سگمنتِ قدیمی یا تک‌قیمتی)، به اشباعِ ثابتِ پیشین برمی‌گردیم.
    /// </summary>
    private static double PriceSaturation(MarketSegmentStat segment, ScoringOptions o)
    {
        if (segment.SpreadPercent <= 0)
        {
            return Math.Max(1, o.BelowMarketSaturation);
        }

        double sat = segment.SpreadPercent * o.SpreadSaturationMultiple;
        return Math.Clamp(sat, Math.Max(1, o.MinPriceSaturationPercent), o.MaxPriceSaturationPercent);
    }

    /// <summary>
    /// عاملِ اطمینان در بازه‌ی [۰..۱]: کفایتِ نمونه × عاملِ پراکندگی × عاملِ سطحِ backoff.
    /// هر سه در نبودِ داده به رفتارِ خنثیِ پیشین (فقط تعدادِ نمونه) برمی‌گردند.
    /// </summary>
    private static double Confidence(MarketSegmentStat segment, ScoringOptions o)
    {
        double sampleAdequacy = Math.Clamp(segment.SampleCount / (double)Math.Max(1, o.ConfidenceFullAtSamples), 0, 1);

        // عاملِ پراکندگی: فشرده (≤ ایده‌آل) → ۱؛ هرچه پراکنده‌تر، تا کفِ تعیین‌شده افت می‌کند. ناملعوم → خنثی.
        double dispersion = 1.0;
        if (segment.SpreadPercent > o.IdealSpreadPercent && o.DispersionSpanPercent > 0)
        {
            double over = segment.SpreadPercent - o.IdealSpreadPercent;
            dispersion = Math.Clamp(1.0 - over / o.DispersionSpanPercent, o.DispersionFloor, 1.0);
        }

        // عاملِ سطح: L0 دقیق → ۱؛ هر سطحِ درشت‌تر ضرب در LevelPenaltyFactor.
        double level = Math.Pow(Math.Clamp(o.LevelPenaltyFactor, 0, 1), segment.Level);

        return Math.Clamp(sampleAdequacy * dispersion * level, 0, 1);
    }

    /// <summary>
    /// قیمتِ موردانتظارِ خودرو با کارکردِ مشخصش: میانه‌ی سگمنت + شیبِ کارکرد × اختلافِ کارکرد (هزارکیلومتر).
    /// اگر شیب/کارکردِ مرجع نبود یا آگهی کارکرد نداشت، همان میانه (رفتارِ پیشین).
    /// </summary>
    private static long ExpectedPrice(CarListing listing, MarketSegmentStat segment)
    {
        long median = segment.MedianPriceToman;
        if (segment.MileageSlopeTomanPerK == 0 || segment.RefMileageKm <= 0 || listing.MileageKm is not { } km)
        {
            return median;
        }

        long deltaK = (km - segment.RefMileageKm) / 1000; // اختلافِ کارکرد به هزارکیلومتر
        long expected = median + segment.MileageSlopeTomanPerK * deltaK;
        return expected > 0 ? expected : median; // ایمنی در برابر مقدارِ نامعقول
    }
}
