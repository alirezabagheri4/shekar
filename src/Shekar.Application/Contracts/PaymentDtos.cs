using Shekar.Domain.Enums;

namespace Shekar.Application.Contracts;

/// <summary>اطلاعات کارت مقصد پرداخت کارت‌به‌کارت (از تنظیمات سراسری خوانده می‌شود، نه هاردکد).</summary>
public sealed record CardTransferInfoDto
{
    /// <summary>شماره کارت بدون فاصله، مثل «6219861082608372».</summary>
    public string CardNumber { get; init; } = string.Empty;

    /// <summary>شماره کارت گروه‌بندی‌شده با ارقام فارسی، مثل «۶۲۱۹ ۸۶۱۰ ۸۲۶۰ ۸۳۷۲».</summary>
    public string CardNumberGrouped { get; init; } = string.Empty;

    /// <summary>نام صاحب کارت.</summary>
    public string HolderName { get; init; } = string.Empty;

    /// <summary>نام بانک.</summary>
    public string BankName { get; init; } = string.Empty;
}

/// <summary>ورودی ثبت پرداخت کارت‌به‌کارت (فایل رسید قبلاً در لایه‌ی Web ذخیره شده).</summary>
public sealed class CardTransferSubmissionDto
{
    public PlanType PlanType { get; set; }
    public PaymentReceiptMethod Method { get; set; }

    /// <summary>کلید فایل رسید (وقتی روش عکس است).</summary>
    public string? ReceiptImageKey { get; set; }

    /// <summary>متن رسید (وقتی روش متن است).</summary>
    public string? ReceiptText { get; set; }

    /// <summary>IP ثبت‌کننده.</summary>
    public string? Ip { get; set; }
}

/// <summary>نتیجه‌ی ثبت پرداخت کارت‌به‌کارت.</summary>
public sealed record CardTransferResultDto
{
    public int PaymentId { get; init; }
    public string Reference { get; init; } = string.Empty;
    public PlanType PlanType { get; init; }

    /// <summary>برچسب شمسی اعتبار اشتراک، مثل «۱۲ آذر ۱۴۰۵».</summary>
    public string ExpiresLabel { get; init; } = string.Empty;
}

/// <summary>یک پرداخت در انتظار بررسی برای صفحه‌ی ادمین.</summary>
public sealed record PendingPaymentDto
{
    public int Id { get; init; }
    public string Reference { get; init; } = string.Empty;
    public string Mobile { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string PlanName { get; init; } = string.Empty;
    public long AmountToman { get; init; }
    public string AmountLabel { get; init; } = string.Empty;
    public PaymentReceiptMethod Method { get; init; }
    public bool HasImage { get; init; }
    public string? ReceiptText { get; init; }
    public string SubmittedAtLabel { get; init; } = string.Empty;
    public string? Ip { get; init; }
}

/// <summary>یک شماره‌ی بلاک‌شده برای صفحه‌ی ادمین.</summary>
public sealed record BlockedPhoneDto
{
    public int Id { get; init; }
    public string Phone { get; init; } = string.Empty;
    public string ReasonLabel { get; init; } = string.Empty;
    public string? Note { get; init; }
    public string BlockedAtLabel { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}
