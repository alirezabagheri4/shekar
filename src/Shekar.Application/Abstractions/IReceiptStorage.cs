namespace Shekar.Application.Abstractions;

/// <summary>
/// ذخیره‌سازی فایل رسیدهای پرداخت. پیاده‌سازی باید فایل‌ها را با نام تصادفی و
/// <b>خارج از پوشه‌ی عمومی wwwroot</b> نگه دارد تا از دسترسی مستقیم جلوگیری شود.
/// </summary>
public interface IReceiptStorage
{
    /// <summary>فایل را ذخیره و کلید یکتای آن را برمی‌گرداند.</summary>
    /// <param name="content">محتوای فایل.</param>
    /// <param name="fileExtension">پسوند با نقطه، مثل «.jpg».</param>
    Task<string> SaveAsync(Stream content, string fileExtension, CancellationToken cancellationToken = default);

    /// <summary>فایل رسید را برای خواندن (نمایش به ادمین) باز می‌کند؛ در صورت نبود <c>null</c>.</summary>
    ReceiptFile? Open(string storageKey);
}

/// <summary>یک فایل رسید بازشده برای استریم به پاسخ.</summary>
public sealed record ReceiptFile(Stream Content, string ContentType);
