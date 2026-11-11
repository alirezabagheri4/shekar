using Microsoft.EntityFrameworkCore;
using Shekar.Domain.Entities;

namespace Shekar.Infrastructure.Persistence;

/// <summary>DbContext اصلی سامانه‌ی شکار (EF Core 6، SQL Server).</summary>
public class ShekarDbContext : DbContext
{
    public ShekarDbContext(DbContextOptions<ShekarDbContext> options) : base(options)
    {
    }

    public DbSet<City> Cities => Set<City>();
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<PlanLimit> PlanLimits => Set<PlanLimit>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<Hunt> Hunts => Set<Hunt>();
    public DbSet<HuntAnalysis> HuntAnalyses => Set<HuntAnalysis>();
    public DbSet<Appraisal> Appraisals => Set<Appraisal>();
    public DbSet<BellAlert> BellAlerts => Set<BellAlert>();
    public DbSet<WaitlistEntry> WaitlistEntries => Set<WaitlistEntry>();
    public DbSet<OtpCode> OtpCodes => Set<OtpCode>();
    public DbSet<Advertiser> Advertisers => Set<Advertiser>();
    public DbSet<BlockedAdvertiser> BlockedAdvertisers => Set<BlockedAdvertiser>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
    public DbSet<Payment> Payments => Set<Payment>();

    // پایپ‌لاین داده (کراولر → اگریگیشن → اسکورینگ)
    public DbSet<VehicleModel> VehicleModels => Set<VehicleModel>();
    public DbSet<CarListing> CarListings => Set<CarListing>();
    public DbSet<MarketSegmentStat> MarketSegmentStats => Set<MarketSegmentStat>();
    public DbSet<CrawlRun> CrawlRuns => Set<CrawlRun>();
    public DbSet<AggregateRun> AggregateRuns => Set<AggregateRun>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ShekarDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
