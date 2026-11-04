using Shekar.Application.Abstractions;

namespace Shekar.Web.Infrastructure;

/// <summary>
/// ذخیره‌سازی فایل رسید روی دیسک، با نام تصادفی و در پوشه‌ای <b>خارج از wwwroot عمومی</b>
/// تا فایل‌ها مستقیماً قابل‌دسترسی نباشند و فقط از مسیر ادمین (احراز هویت‌شده) استریم شوند.
/// مسیر پیش‌فرض: «&lt;ContentRoot&gt;/App_Data/receipts»؛ با تنظیم «ReceiptStorage:Path» قابل تغییر.
/// </summary>
public sealed class LocalReceiptStorage : IReceiptStorage
{
    private readonly string _root;

    public LocalReceiptStorage(IWebHostEnvironment env, IConfiguration config)
    {
        string? configured = config["ReceiptStorage:Path"];
        _root = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(env.ContentRootPath, "App_Data", "receipts")
            : configured;
        Directory.CreateDirectory(_root);
    }

    public async Task<string> SaveAsync(Stream content, string fileExtension, CancellationToken cancellationToken = default)
    {
        string ext = Normalize(fileExtension);
        string key = Guid.NewGuid().ToString("N") + ext;
        string full = Path.Combine(_root, key);

        await using FileStream file = File.Create(full);
        await content.CopyToAsync(file, cancellationToken);
        return key;
    }

    public ReceiptFile? Open(string storageKey)
    {
        // از path traversal جلوگیری کن: فقط نام فایل، بدون جداکننده‌ی مسیر.
        if (string.IsNullOrWhiteSpace(storageKey)
            || storageKey.Contains('/') || storageKey.Contains('\\') || storageKey.Contains(".."))
        {
            return null;
        }

        string full = Path.Combine(_root, storageKey);
        if (!File.Exists(full))
        {
            return null;
        }

        Stream stream = File.OpenRead(full);
        return new ReceiptFile(stream, ContentType(Path.GetExtension(full)));
    }

    private static string Normalize(string ext)
    {
        ext = ext.Trim().ToLowerInvariant();
        if (!ext.StartsWith('.'))
        {
            ext = "." + ext;
        }

        return ext is ".jpg" or ".jpeg" or ".png" or ".webp" ? ext : ".bin";
    }

    private static string ContentType(string ext) => ext.ToLowerInvariant() switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".webp" => "image/webp",
        _ => "application/octet-stream"
    };
}
