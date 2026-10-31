using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Shekar.Application.Services;

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

        using IServiceScope scope = _scopeFactory.CreateScope();
        IServiceProvider sp = scope.ServiceProvider;

        int matched = await sp.GetRequiredService<IVehicleNormalizationService>().RunAsync(normalizeBatch, ct);
        int segments = await sp.GetRequiredService<IMarketAggregationService>().RunAsync(windowDays, ct);

        _logger.LogInformation("اگریگیشن: نرمال‌سازی {Matched} آگهی، ساخت {Segments} سگمنت بازار", matched, segments);
    }
}
