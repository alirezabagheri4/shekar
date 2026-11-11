using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Shekar.Application.Abstractions;
using Shekar.Application.Services;
using Shekar.Domain.Entities;
using Shekar.Domain.Enums;

namespace Shekar.Worker.Jobs;

/// <summary>
/// Job ساعتی (مرحله ۲ پایپ‌لاین): ابتدا آگهی‌های نرمال‌نشده را به مدل کاتالوگ نگاشت می‌کند،
/// سپس آمار سگمنت‌های بازار (میانه/چارک) را می‌سازد.
/// </summary>
[DisallowConcurrentExecution]
public sealed class AggregateJob : IJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<AggregateJob> _logger;

    public AggregateJob(IServiceScopeFactory scopeFactory, IConfiguration config, ILogger<AggregateJob> logger)
    {
        _scopeFactory = scopeFactory;
        _config = config;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        CancellationToken ct = context.CancellationToken;
        int windowDays = _config.GetValue<int?>("Aggregate:WindowDays") ?? 30;
        int normalizeBatch = _config.GetValue<int?>("Aggregate:NormalizeBatch") ?? 5000;

        var aggOptions = new AggregationOptions
        {
            MinReliableSamples = _config.GetValue<int?>("Aggregate:MinReliableSamples") ?? 5,
            MaxReliableSpreadPercent = _config.GetValue<int?>("Aggregate:MaxReliableSpreadPercent") ?? 60,
            RecencyHalfLifeDays = _config.GetValue<double?>("Aggregate:RecencyHalfLifeDays") ?? 14
        };

        using IServiceScope scope = _scopeFactory.CreateScope();
        IServiceProvider sp = scope.ServiceProvider;
        var runs = sp.GetRequiredService<IAggregateRunRepository>();

        AggregateRun run = await runs.AddAsync(new AggregateRun { StartedAt = DateTimeOffset.UtcNow }, ct);
        try
        {
            NormalizationResult norm = await sp.GetRequiredService<IVehicleNormalizationService>().RunAsync(normalizeBatch, ct);
            AggregationResult agg = await sp.GetRequiredService<IMarketAggregationService>().RunAsync(windowDays, aggOptions, ct);

            run.NormalizedCount = norm.Matched;
            run.AutoRegistered = norm.AutoRegistered;
            run.UnmatchedCount = norm.Unmatched;
            run.SegmentCount = agg.Segments;
            run.ReliableSegmentCount = agg.Reliable;
            run.StaledCount = agg.Staled;
            run.Status = CrawlStatus.Succeeded;

            _logger.LogInformation(
                "اگریگیشن: نرمال‌سازی {Matched} (رجیستر خودکار {Auto})، بی‌تطبیق {Unmatched}، سگمنت {Segments} (قابل‌اتکا {Reliable}، کهنه‌شده {Staled})",
                norm.Matched, norm.AutoRegistered, norm.Unmatched, agg.Segments, agg.Reliable, agg.Staled);

            if (norm.UnmatchedSamples.Count > 0)
            {
                _logger.LogWarning(
                    "پوشش: {Count} آگهی بی‌تطبیق ماند (نمونه): {Samples}",
                    norm.Unmatched, string.Join(" | ", norm.UnmatchedSamples));
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            run.Status = CrawlStatus.Failed;
            run.Error = ex.Message;
            _logger.LogError(ex, "خطای اگریگیشن");
        }
        finally
        {
            run.FinishedAt = DateTimeOffset.UtcNow;
            await runs.UpdateAsync(run, ct);
        }
    }
}
