using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Shekar.Application.Services;

namespace Shekar.Worker.Jobs;

/// <summary>
/// Job اسکورینگ (مرحله ۳ پایپ‌لاین): آگهی‌های نرمال‌شده‌ی اخیر را با میانه‌ی سگمنت می‌سنجد
/// و واجدین شرایط را در تاپ‌ها (Hunts) درج/به‌روزرسانی می‌کند.
/// </summary>
[DisallowConcurrentExecution]
public sealed class ScoreJob : IJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<ScoreJob> _logger;

    public ScoreJob(IServiceScopeFactory scopeFactory, IConfiguration config, ILogger<ScoreJob> logger)
    {
        _scopeFactory = scopeFactory;
        _config = config;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        CancellationToken ct = context.CancellationToken;
        int windowHours = _config.GetValue<int?>("Score:WindowHours") ?? 48;

        var options = new ScoringOptions
        {
            MinBelowMarketPercent = _config.GetValue<int?>("Score:MinBelowMarketPercent") ?? 5,
            MinScore = _config.GetValue<int?>("Score:MinScore") ?? 60,
            MinSamples = _config.GetValue<int?>("Score:MinSamples") ?? 3,
            SuspiciousBelowMarketPercent = _config.GetValue<int?>("Score:SuspiciousBelowMarketPercent") ?? 40,
            ConfidenceFullAtSamples = _config.GetValue<int?>("Score:ConfidenceFullAtSamples") ?? 5,
            RisingMarketTrendPercent = _config.GetValue<int?>("Score:RisingMarketTrendPercent") ?? 5,
            RisingMarketExtraMarginPercent = _config.GetValue<int?>("Score:RisingMarketExtraMarginPercent") ?? 3,
            SpreadSaturationMultiple = _config.GetValue<double?>("Score:SpreadSaturationMultiple") ?? 1.0,
            MinPriceSaturationPercent = _config.GetValue<int?>("Score:MinPriceSaturationPercent") ?? 8,
            MaxPriceSaturationPercent = _config.GetValue<int?>("Score:MaxPriceSaturationPercent") ?? 35,
            IdealSpreadPercent = _config.GetValue<int?>("Score:IdealSpreadPercent") ?? 15,
            DispersionSpanPercent = _config.GetValue<int?>("Score:DispersionSpanPercent") ?? 45,
            DispersionFloor = _config.GetValue<double?>("Score:DispersionFloor") ?? 0.30,
            LevelPenaltyFactor = _config.GetValue<double?>("Score:LevelPenaltyFactor") ?? 0.85,
            RequireReliableForGolden = _config.GetValue<bool?>("Score:RequireReliableForGolden") ?? true
        };

        using IServiceScope scope = _scopeFactory.CreateScope();
        var scoring = scope.ServiceProvider.GetRequiredService<IListingScoringService>();
        ScoringRunResult r = await scoring.RunAsync(options, windowHours, ct);

        _logger.LogInformation(
            "اسکورینگ: ارزیابی {Evaluated}، واجد {Qualified}، ساخت {Created}، به‌روز {Updated}، حذف {Removed}، مشکوک {Suspicious}، تکراری {Duplicates}",
            r.Evaluated, r.Qualified, r.Created, r.Updated, r.Removed, r.Suspicious, r.Duplicates);
    }
}
