using Shekar.Application.Contracts;

namespace Shekar.Web.Models;

public sealed record HuntsViewModel(
    HuntListDto List,
    IReadOnlyList<CategorySummaryDto> Categories,
    SubscriptionStatusDto Status,
    DateTimeOffset Now);

public sealed record HuntDetailViewModel(HuntDetailDto Detail, DateTimeOffset Now);

public sealed record CategoriesViewModel(
    IReadOnlyList<CategorySummaryDto> Categories,
    int TodayTotal,
    SubscriptionStatusDto Status);

public sealed record AppraiseViewModel(AppraisalQuotaDto Quota);

public sealed record PaySuccessViewModel(
    string PlanName,
    string Amount,
    string ExpiresLabel,
    string TrackingCode,
    bool IsTrial);

/// <summary>صفحه‌ی پرداخت کارت‌به‌کارت: اطلاعات کارت مقصد + پلن انتخابی اولیه.</summary>
public sealed record PayViewModel(CardTransferInfoDto Card, string Plan);

/// <summary>صفحه‌ی بررسی واریزی: پلن، کد پیگیری و اعتبار اشتراک.</summary>
public sealed record PayReviewViewModel(string Plan, string Reference, string ExpiresLabel);

/// <summary>صفحه‌ی ادمین: پرداخت‌های در انتظار بررسی + شماره‌های بلاک‌شده.</summary>
public sealed record AdminPaymentsViewModel(
    IReadOnlyList<PendingPaymentDto> Pending,
    IReadOnlyList<BlockedPhoneDto> Blocked);

public sealed record LoginOtpViewModel(string Mobile);

public sealed record LoginProfileViewModel(IReadOnlyList<(int Id, string Name, bool IsActive)> Cities);

public sealed record PlansViewModel(
    IReadOnlyList<PlanDto> Plans,
    CityCapacityDto Capacity);

public sealed record AccountViewModel(
    SubscriptionStatusDto Status,
    string FullName,
    string Mobile,
    string CityName,
    string ExpiresLabel,
    int DaysRemaining,
    IReadOnlyList<BellToggleResultDto> Bells,
    IReadOnlyList<AppraisalHistoryItemDto> Appraisals,
    IReadOnlyList<CategorySummaryDto> Categories);
