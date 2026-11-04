using Shekar.Domain.Enums;

namespace Shekar.Domain.Entities;

/// <summary>
/// یک درخواست پرداخت کارت‌به‌کارت: کاربر مبلغ پلن را واریز کرده و رسید (عکس یا متن) فرستاده است.
/// اشتراک همان لحظه فعال می‌شود و پرداخت در وضعیت «در انتظار بررسی» می‌ماند تا ادمین آن را
/// تأیید یا به‌عنوان رسید جعلی رد کند.
/// </summary>
public class Payment
{
    public int Id { get; set; }

    /// <summary>کاربر پرداخت‌کننده.</summary>
    public int UserId { get; set; }

    /// <summary>شماره موبایل کاربر در لحظه‌ی پرداخت (برای بلاک در صورت جعلی‌بودن).</summary>
    public string Mobile { get; set; } = string.Empty;

    /// <summary>پلن خریداری‌شده.</summary>
    public PlanType PlanType { get; set; }

    /// <summary>شهر اشتراک (مبنای ظرفیت R1).</summary>
    public int CityId { get; set; }

    /// <summary>مبلغ واریزی به تومان (از قیمت پلن).</summary>
    public long AmountToman { get; set; }

    /// <summary>روش ارسال رسید: عکس یا متن.</summary>
    public PaymentReceiptMethod Method { get; set; }

    /// <summary>کلید فایل رسید (نام تصادفی خارج از wwwroot عمومی)؛ فقط وقتی روش «عکس» است.</summary>
    public string? ReceiptImageKey { get; set; }

    /// <summary>متن رسید؛ فقط وقتی روش «متن» است.</summary>
    public string? ReceiptText { get; set; }

    /// <summary>کد پیگیری نمایشی، مثل «SHK-482193».</summary>
    public string Reference { get; set; } = string.Empty;

    /// <summary>IP ثبت‌کننده‌ی پرداخت.</summary>
    public string? Ip { get; set; }

    /// <summary>زمان ثبت پرداخت.</summary>
    public DateTimeOffset SubmittedAt { get; set; }

    /// <summary>وضعیت بررسی.</summary>
    public PaymentReviewStatus Status { get; set; } = PaymentReviewStatus.Pending;

    /// <summary>اشتراکی که با این پرداخت فعال/تمدید شد (برای غیرفعال‌سازی هنگام رد).</summary>
    public int? SubscriptionId { get; set; }

    /// <summary>زمان بررسی توسط ادمین.</summary>
    public DateTimeOffset? ReviewedAt { get; set; }

    /// <summary>شناسه‌ی ادمین بررسی‌کننده.</summary>
    public int? ReviewedByUserId { get; set; }

    /// <summary>توضیح بررسی (مثلاً دلیل رد).</summary>
    public string? ReviewNote { get; set; }
}
