using Shekar.Application.Contracts;
using Shekar.Domain.Enums;

namespace Shekar.Application.Services;

/// <summary>لیست و جزئیات شکارها (R4، R7، R8، R9، R11).</summary>
public interface IHuntService
{
    Task<HuntListDto> GetListAsync(PriceCategory? category, HuntDay day, HuntSortOrder sort, CancellationToken ct = default);
    Task<HuntDetailDto> GetDetailAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<CategorySummaryDto>> GetCategoriesAsync(CancellationToken ct = default);
}

/// <summary>کارشناسی قیمت (R5).</summary>
public interface IAppraisalService
{
    Task<AppraisalQuotaDto> GetQuotaAsync(CancellationToken ct = default);
    Task<AppraisalResultDto> AppraiseAsync(AppraisalRequestDto request, CancellationToken ct = default);
    Task<IReadOnlyList<AppraisalHistoryItemDto>> GetHistoryAsync(CancellationToken ct = default);
}

/// <summary>اشتراک‌ها (R1، R2، R3).</summary>
public interface ISubscriptionService
{
    Task<SubscriptionStatusDto> GetStatusAsync(CancellationToken ct = default);
    Task<SubscriptionStatusDto> GetStatusForUserAsync(int userId, CancellationToken ct = default);
    Task<PurchaseResultDto> StartTrialAsync(StartSubscriptionRequestDto request, CancellationToken ct = default);
    Task<PurchaseResultDto> PurchaseAsync(StartSubscriptionRequestDto request, CancellationToken ct = default);
}

/// <summary>ظرفیت شهر (R1).</summary>
public interface ICapacityService
{
    Task<CityCapacityDto> GetAsync(int cityId, CancellationToken ct = default);
}

/// <summary>لیست انتظار (R1).</summary>
public interface IWaitlistService
{
    Task<WaitlistPositionDto> JoinAsync(WaitlistJoinRequestDto request, CancellationToken ct = default);
}

/// <summary>زنگوله‌ی «خبرم کن» (R6).</summary>
public interface IBellService
{
    Task<IReadOnlyList<BellToggleResultDto>> ListAsync(CancellationToken ct = default);
    Task<BellToggleResultDto> ToggleAsync(BellToggleRequestDto request, CancellationToken ct = default);
}

/// <summary>پلن‌ها.</summary>
public interface IPlanService
{
    Task<IReadOnlyList<PlanDto>> ListAsync(CancellationToken ct = default);
}

/// <summary>ورود با موبایل + OTP.</summary>
public interface IAuthService
{
    Task RequestOtpAsync(RequestOtpDto request, CancellationToken ct = default);
    Task<AuthResultDto> VerifyOtpAsync(VerifyOtpDto request, CancellationToken ct = default);
    Task CompleteProfileAsync(int userId, CompleteProfileDto request, CancellationToken ct = default);
}

/// <summary>ساعت پشتیبانی (R12).</summary>
public interface ISupportService
{
    bool IsOpenNow();
    string HoursLabel { get; }
    string StatusLabel { get; }
}

/// <summary>پرداخت کارت‌به‌کارت: ثبت واریزی و فعال‌سازی اشتراک + بررسی ادمین.</summary>
public interface IPaymentService
{
    /// <summary>اطلاعات کارت مقصد (از تنظیمات سراسری).</summary>
    Task<CardTransferInfoDto> GetCardInfoAsync(CancellationToken ct = default);

    /// <summary>ثبت پرداخت کارت‌به‌کارت کاربر جاری و فعال‌سازی/تمدید اشتراک (منتظر تأیید).</summary>
    Task<CardTransferResultDto> SubmitCardTransferAsync(CardTransferSubmissionDto request, CancellationToken ct = default);

    /// <summary>برچسب شمسی اعتبار اشتراک فعال کاربر جاری (برای صفحه‌ی بررسی)؛ در صورت نبود رشته‌ی خالی.</summary>
    Task<string> GetActiveExpiresLabelAsync(CancellationToken ct = default);

    // --- بررسی ادمین ---

    Task<IReadOnlyList<PendingPaymentDto>> ListPendingAsync(CancellationToken ct = default);

    /// <summary>تأیید پرداخت: وضعیت تأییدشده می‌شود (اشتراک از قبل فعال است).</summary>
    Task ApproveAsync(int paymentId, int adminUserId, CancellationToken ct = default);

    /// <summary>رد پرداخت (رسید جعلی): اشتراک غیرفعال و شماره‌ی کاربر بلاک می‌شود.</summary>
    Task RejectAsync(int paymentId, int adminUserId, CancellationToken ct = default);

    Task<IReadOnlyList<BlockedPhoneDto>> ListBlockedAsync(CancellationToken ct = default);

    /// <summary>رفع بلاک یک شماره توسط ادمین.</summary>
    Task UnblockAsync(int blockedId, int adminUserId, CancellationToken ct = default);
}
