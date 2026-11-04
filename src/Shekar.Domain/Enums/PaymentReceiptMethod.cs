namespace Shekar.Domain.Enums;

/// <summary>روش ارسال رسید پرداخت کارت‌به‌کارت.</summary>
public enum PaymentReceiptMethod
{
    /// <summary>آپلود عکس رسید.</summary>
    Image = 1,

    /// <summary>چسباندن متن پیامک بانک / رسید اینترنتی.</summary>
    Text = 2
}
