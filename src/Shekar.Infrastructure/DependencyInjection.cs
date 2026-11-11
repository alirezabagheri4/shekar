using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shekar.Application.Abstractions;
using Shekar.Infrastructure.Crawling;
using Shekar.Infrastructure.Persistence;
using Shekar.Infrastructure.Repositories;

namespace Shekar.Infrastructure;

/// <summary>ثبت لایه‌ی دیتابیس واقعی (EF Core 6 + SQL Server) و Repositoryها.</summary>
public static class DependencyInjection
{
    /// <summary>کراولر واقعی دیوار + موتور درخواستِ مؤدب (ضدبلاک). برای میزبان Worker.</summary>
    public static IServiceCollection AddDivarCrawler(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<PoliteFetchOptions>(config.GetSection("Crawl:Polite"));
        services.AddSingleton<IWebFetcher, PoliteWebFetcher>();

        services.AddHttpClient("crawler")
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
                UseCookies = true,                 // رفتار طبیعیِ نشست مرورگر
                CookieContainer = new CookieContainer()
            });

        services.AddSingleton<IListingSourceProvider, DivarHtmlProvider>();
        return services;
    }

    public static IServiceCollection AddSqlServerInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<ShekarDbContext>(options =>
            options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));

        services.AddScoped<ICityRepository, EfCityRepository>();
        services.AddScoped<IPlanRepository, EfPlanRepository>();
        services.AddScoped<IUserRepository, EfUserRepository>();
        services.AddScoped<ISubscriptionRepository, EfSubscriptionRepository>();
        services.AddScoped<IHuntRepository, EfHuntRepository>();
        services.AddScoped<IAppraisalRepository, EfAppraisalRepository>();
        services.AddScoped<IBellAlertRepository, EfBellAlertRepository>();
        services.AddScoped<IWaitlistRepository, EfWaitlistRepository>();
        services.AddScoped<IOtpRepository, EfOtpRepository>();
        services.AddScoped<IAdvertiserRepository, EfAdvertiserRepository>();
        services.AddScoped<IBlockedAdvertiserRepository, EfBlockedAdvertiserRepository>();
        services.AddScoped<IPlanLimitRepository, EfPlanLimitRepository>();
        services.AddScoped<ISystemSettingRepository, EfSystemSettingRepository>();
        services.AddScoped<ICarListingRepository, EfCarListingRepository>();
        services.AddScoped<ICrawlRunRepository, EfCrawlRunRepository>();
        services.AddScoped<IAggregateRunRepository, EfAggregateRunRepository>();
        services.AddScoped<IVehicleModelRepository, EfVehicleModelRepository>();
        services.AddScoped<IMarketSegmentStatRepository, EfMarketSegmentStatRepository>();
        services.AddScoped<IPaymentRepository, EfPaymentRepository>();

        return services;
    }
}
