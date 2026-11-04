namespace Shekar.Domain.Enums;

/// <summary>دلیل بلاک‌شدن یک شماره‌ی آگهی‌دهنده از لیست پیشنهادهای روزانه.</summary>
public enum BlockReason
{
    /// <summary>تعداد آگهی بیش از حد مجاز (اسپم).</summary>
    TooManyAds = 1,

    /// <summary>دلال / واسطه.</summary>
    Broker = 2,

    /// <summary>اطلاعات نادرست یا گمراه‌کننده در آگهی.</summary>
    FalseInformation = 3,

    /// <summary>آگهی تکراری.</summary>
    Duplicate = 4,

    /// <summary>گزارش کاربران.</summary>
    Reported = 5,

    /// <summary>بلاک دستی توسط ادمین.</summary>
    Manual = 6,

    /// <summary>رسید پرداخت جعلی (رد پرداخت کارت‌به‌کارت توسط ادمین).</summary>
    FakeReceipt = 7
}
