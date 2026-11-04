namespace Shekar.Domain.Enums;

/// <summary>وضعیت بررسی پرداخت کارت‌به‌کارت توسط ادمین.</summary>
public enum PaymentReviewStatus
{
    /// <summary>در انتظار بررسی (اشتراک موقتاً فعال شده).</summary>
    Pending = 1,

    /// <summary>تأییدشده توسط ادمین.</summary>
    Approved = 2,

    /// <summary>ردشده (رسید جعلی): اشتراک غیرفعال و شماره بلاک می‌شود.</summary>
    Rejected = 3
}
