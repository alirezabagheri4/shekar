using Microsoft.Extensions.DependencyInjection;
using Shekar.Application.Abstractions;
using Shekar.Mock.Providers;
using Shekar.Mock.Repositories;
using Shekar.Mock.Services;

namespace Shekar.Mock;

/// <summary>ثبت پیاده‌سازی‌های ماک (داده + زیرساخت) به‌عنوان پیش‌فرض.</summary>
public static class DependencyInjection
{
    /// <summary>کل لایه‌ی Mock: سرویس‌های زیرساختی + Repositoryهای درون‌حافظه‌ای.</summary>
    public static IServiceCollection AddMockInfrastructure(this IServiceCollection services)
    {
        services.AddMockSupportServices();
        services.AddMockRepositories();
        return services;
    }

    /// <summary>سرویس‌های زیرساختی غیر-دیتابیسی (ساعت، تقویم، پیامک، OTP، اعتبارسنج لینک، موتور کارشناسی).</summary>
    public static IServiceCollection AddMockSupportServices(this IServiceCollection services)
    {
        services.AddSingleton<IClock, TehranClock>();
        services.AddSingleton<IJalaliCalendar, PersianJalaliCalendar>();
        services.AddSingleton<ISmsSender, MockSmsSender>();
        services.AddSingleton<IOtpGenerator, FixedOtpGenerator>();
        services.AddSingleton<IAdLinkValidator, CarAdLinkValidator>();
        services.AddSingleton<IAppraisalEngine, MockAppraisalEngine>();
        return services;
    }

    /// <summary>Repositoryهای درون‌حافظه‌ای روی مخزن singleton.</summary>
    public static IServiceCollection AddMockRepositories(this IServiceCollection services)
    {
        services.AddSingleton<MockDataStore>();

        services.AddScoped<ICityRepository, MockCityRepository>();
        services.AddScoped<IPlanRepository, MockPlanRepository>();
        services.AddScoped<IUserRepository, MockUserRepository>();
        services.AddScoped<ISubscriptionRepository, MockSubscriptionRepository>();
        services.AddScoped<IHuntRepository, MockHuntRepository>();
        services.AddScoped<IAppraisalRepository, MockAppraisalRepository>();
        services.AddScoped<IBellAlertRepository, MockBellAlertRepository>();
        services.AddScoped<IWaitlistRepository, MockWaitlistRepository>();
        services.AddScoped<IOtpRepository, MockOtpRepository>();
        services.AddScoped<IAdvertiserRepository, MockAdvertiserRepository>();
        services.AddScoped<IBlockedAdvertiserRepository, MockBlockedAdvertiserRepository>();
        services.AddScoped<IPlanLimitRepository, MockPlanLimitRepository>();
        services.AddScoped<ISystemSettingRepository, MockSystemSettingRepository>();
        services.AddScoped<ICarListingRepository, MockCarListingRepository>();
        services.AddScoped<ICrawlRunRepository, MockCrawlRunRepository>();
        services.AddScoped<IVehicleModelRepository, MockVehicleModelRepository>();
        services.AddScoped<IMarketSegmentStatRepository, MockMarketSegmentStatRepository>();
        return services;
    }

    /// <summary>منبع آگهی ماک (کراولر بدون کراول واقعی) برای پروژه‌ی Worker.</summary>
    public static IServiceCollection AddMockCrawlProvider(this IServiceCollection services)
    {
        services.AddSingleton<IListingSourceProvider, MockListingSourceProvider>();
        return services;
    }
}
