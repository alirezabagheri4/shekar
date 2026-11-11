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

    /// <summary>درصد زیر بازاری که مؤلفه‌ی قیمت را اشباع می‌کند (۲۰٪ → کامل).</summary>
    public int BelowMarketSaturation { get; init; } = 20;

    /// <summary>
    /// سقفِ منطقیِ «زیر بازار»؛ بالاتر از آن آگهی مشکوک است (کلاه‌برداری/اقساطی/قیمتِ تایپی)
    /// و نباید به‌عنوان فرصت (به‌ویژه طلایی) نمایش داده شود. به‌جای آن «مشکوک» علامت می‌خورد.
    /// </summary>
    public int SuspiciousBelowMarketPercent { get; init; } = 40;

    /// <summary>تعداد نمونه‌ای که مؤلفه‌ی اطمینان را کامل می‌کند (۵ نمونه → ۱۰۰).</summary>
    public int ConfidenceFullAtSamples { get; init; } = 5;

    public double PriceWeight { get; init; } = 0.55;
    public double LiquidityWeight { get; init; } = 0.35;
    public double ConfidenceWeight { get; init; } = 0.10;
}

/// <summary>نتیجه‌ی اسکورِ یک آگهی نسبت به سگمنتش.</summary>
public sealed record ScoreResult(
    bool Qualifies, int Score, int BelowMarketPercent, long EstimatedProfitToman, Liquidity Liquidity,
    bool IsGolden, bool IsSuspicious);

/// <summary>اسکورر شکار (آماری/هیوریستیک)؛ پشت interface تا بعداً با مدل ML جایگزین شود.</summary>
public interface IHuntScorer
{
    ScoreResult Score(CarListing listing, MarketSegmentStat segment, VehicleModel model, ScoringOptions options);
}

public sealed class HuntScorer : IHuntScorer
{
    public ScoreResult Score(CarListing listing, MarketSegmentStat segment, VehicleModel model, ScoringOptions o)
    {
        long median = segment.MedianPriceToman;
        int belowPct = median > 0 ? (int)Math.Round((median - listing.PriceToman) * 100.0 / median) : 0;

        long profit = HuntCalculations.EstimatedNetProfit(median, listing.PriceToman);

        // مؤلفه‌ها (۰..۱۰۰):
        double priceComp = Math.Clamp(belowPct * 100.0 / Math.Max(1, o.BelowMarketSaturation), 0, 100);
        double liqComp = Math.Clamp(model.LiquidityScore, 0, 100);
        double confComp = Math.Clamp(segment.SampleCount * 100.0 / Math.Max(1, o.ConfidenceFullAtSamples), 0, 100);

        int score = (int)Math.Round(o.PriceWeight * priceComp + o.LiquidityWeight * liqComp + o.ConfidenceWeight * confComp);
        score = Math.Clamp(score, 0, 100);

        Liquidity liquidity = model.LiquidityScore >= 75 ? Liquidity.High : Liquidity.Medium;

        // محافظِ کلاه‌برداری/پرت: زیربازارِ بیش از سقفِ منطقی، آگهیِ «مشکوک» است؛ طلایی نمی‌شود
        // و به‌عنوان فرصت واجد شرایط نیست (کاربر را به سمت کلاه‌برداری نمی‌بریم).
        bool suspicious = belowPct >= o.SuspiciousBelowMarketPercent;
        bool golden = score >= DomainConstants.GoldenScoreThreshold && !suspicious;

        bool qualifies = !suspicious
            && belowPct >= o.MinBelowMarketPercent
            && segment.SampleCount >= o.MinSamples
            && score >= o.MinScore;

        return new ScoreResult(qualifies, score, belowPct, profit, liquidity, golden, suspicious);
    }
}
