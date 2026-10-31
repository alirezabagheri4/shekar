using Microsoft.Extensions.DependencyInjection;
using Shekar.Application.Services;

namespace Shekar.Application;

/// <summary>ثبت سرویس‌های لایه‌ی Application در DI.</summary>
public static class DependencyInjection
{
    /// <summary>سرویس‌های کاربرپسند (نیازمند ICurrentUser) + سرویس‌های پایپ‌لاین. برای میزبان Web.</summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IHuntService, HuntService>();
        services.AddScoped<IAppraisalService, AppraisalService>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();
        services.AddScoped<ICapacityService, CapacityService>();
        services.AddScoped<IWaitlistService, WaitlistService>();
        services.AddScoped<IBellService, BellService>();
        services.AddScoped<IPlanService, PlanService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ISupportService, SupportService>();

        services.AddPipelineServices();
        return services;
    }

    /// <summary>سرویس‌های پایپ‌لاین داده (بدون وابستگی به ICurrentUser). برای میزبان Worker.</summary>
    public static IServiceCollection AddPipelineServices(this IServiceCollection services)
    {
        services.AddScoped<IListingIngestionService, ListingIngestionService>();
        services.AddSingleton<IVehicleModelMatcher, VehicleModelMatcher>();
        services.AddScoped<IVehicleNormalizationService, VehicleNormalizationService>();
        services.AddScoped<IMarketAggregationService, MarketAggregationService>();
        return services;
    }
}
