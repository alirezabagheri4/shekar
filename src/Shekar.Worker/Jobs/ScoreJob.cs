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
            ConfidenceFullAtSamples = _config.GetValue<int?>("Score:ConfidenceFullAtSamples") ?? 5
        };

        using IServiceScope scope = _scopeFactory.CreateScope();
        var scoring = scope.ServiceProvider.GetRequiredService<IListingScoringService>();
        ScoringRunResult r = await scoring.RunAsync(options, windowHours, ct);

        _logger.LogInformation(
            "اسکورینگ: ارزیابی {Evaluated}، واجد {Qualified}، ساخت {Created}، به‌روز {Updated}، حذف {Removed}، مشکوک {Suspicious}، تکراری {Duplicates}",
            r.Evaluated, r.Qualified, r.Created, r.Updated, r.Removed, r.Suspicious, r.Duplicates);
    }
}
