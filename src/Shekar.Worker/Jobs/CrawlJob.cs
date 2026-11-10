using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Shekar.Application.Abstractions;
using Shekar.Application.Services;
using Shekar.Domain.Entities;

namespace Shekar.Worker.Jobs;

/// <summary>
/// Job کراول (مرحله ۱ پایپ‌لاین): برای هر شهر فعال و هر منبع، آگهی‌ها را دریافت،
/// فیلتر و ذخیره می‌کند. هر پاس در CrawlRuns لاگ می‌شود.
/// </summary>
[DisallowConcurrentExecution]
public sealed class CrawlJob : IJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<CrawlJob> _logger;

    public CrawlJob(IServiceScopeFactory scopeFactory, IConfiguration config, ILogger<CrawlJob> logger)
    {
        _scopeFactory = scopeFactory;
        _config = config;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        CancellationToken ct = context.CancellationToken;
        int maxAgeHours = _config.GetValue<int?>("Crawl:MaxAgeHours") ?? 24;
        int staleAfterHours = _config.GetValue<int?>("Crawl:StaleAfterHours") ?? 72;

        // بی‌نظمیِ انسان‌مانند: هر پاس با تأخیرِ تصادفی شروع شود تا زمان‌بندی دقیقاً دوره‌ای نباشد.
        int jitterSeconds = _config.GetValue<int?>("Crawl:StartJitterSeconds") ?? 0;
        if (jitterSeconds > 0)
        {
            await Task.Delay(TimeSpan.FromSeconds(Random.Shared.Next(0, jitterSeconds + 1)), ct);
        }

        using IServiceScope scope = _scopeFactory.CreateScope();
        IServiceProvider sp = scope.ServiceProvider;

        var cities = await sp.GetRequiredService<ICityRepository>().ListAsync(ct);
        var providers = sp.GetServices<IListingSourceProvider>().ToList();
        var ingestion = sp.GetRequiredService<IListingIngestionService>();

        foreach (City city in cities.Where(c => c.IsActive))
        {
            foreach (IListingSourceProvider provider in providers)
            {
                try
                {
                    IngestionResult r = await ingestion.RunAsync(provider, city.Id, maxAgeHours, staleAfterHours, ct);
                    _logger.LogInformation(
                        "کراول {Source}/{City}: دریافت {Fetched}، ذخیره {Saved}، ردشده {Skipped}، حذف {Removed}، خطا {Errors}",
                        provider.Source, city.Name, r.Fetched, r.Saved, r.Skipped, r.Removed, r.Errors);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "خطای کراول {Source}/{City}", provider.Source, city.Name);
                }
            }
        }
    }
}
