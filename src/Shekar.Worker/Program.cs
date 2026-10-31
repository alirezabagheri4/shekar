using Quartz;
using Shekar.Application;
using Shekar.Infrastructure;
using Shekar.Mock;
using Shekar.Worker.Jobs;

var builder = Host.CreateApplicationBuilder(args);

// فقط سرویس‌های پایپ‌لاین (بدون سرویس‌های کاربرپسندِ وابسته به ICurrentUser).
builder.Services.AddPipelineServices();

string persistence = builder.Configuration["Persistence"] ?? "Mock";
if (string.Equals(persistence, "SqlServer", StringComparison.OrdinalIgnoreCase))
{
    string conn = builder.Configuration.GetConnectionString("Shekar")
        ?? throw new InvalidOperationException("ConnectionStrings:Shekar تنظیم نشده است.");
    builder.Services.AddMockSupportServices();
    builder.Services.AddSqlServerInfrastructure(conn);
}
else
{
    builder.Services.AddMockInfrastructure();
}

// منبع آگهی (فعلاً ماک؛ بعداً provider واقعیِ دیوار جایگزین می‌شود).
builder.Services.AddMockCrawlProvider();

// زمان‌بندی با Quartz — کراول هر N دقیقه، اگریگیشن هر M دقیقه (پیش‌فرض ساعتی).
int crawlMinutes = Math.Max(1, builder.Configuration.GetValue<int?>("Crawl:IntervalMinutes") ?? 5);
int aggregateMinutes = Math.Max(1, builder.Configuration.GetValue<int?>("Aggregate:IntervalMinutes") ?? 60);
builder.Services.AddQuartz(q =>
{
    var crawlJob = new JobKey("CrawlJob");
    q.AddJob<CrawlJob>(o => o.WithIdentity(crawlJob));
    q.AddTrigger(t => t
        .ForJob(crawlJob)
        .WithIdentity("CrawlJob-trigger")
        .StartNow()
        .WithSimpleSchedule(s => s.WithIntervalInMinutes(crawlMinutes).RepeatForever()));

    var aggregateJob = new JobKey("AggregateJob");
    q.AddJob<AggregateJob>(o => o.WithIdentity(aggregateJob));
    q.AddTrigger(t => t
        .ForJob(aggregateJob)
        .WithIdentity("AggregateJob-trigger")
        .StartAt(DateTimeOffset.UtcNow.AddSeconds(20))   // کمی بعد از اولین کراول
        .WithSimpleSchedule(s => s.WithIntervalInMinutes(aggregateMinutes).RepeatForever()));
});
builder.Services.AddQuartzHostedService(o => o.WaitForJobsToComplete = true);

IHost host = builder.Build();
host.Run();
