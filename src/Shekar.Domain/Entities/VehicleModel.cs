namespace Shekar.Domain.Entities;

/// <summary>
/// کاتالوگ خودرو؛ مرجعِ نرمال‌سازی آگهی‌ها و نگهداری «نقدشوندگیِ از پیش‌تعیین‌شده» هر مدل.
/// مثلاً «پژو ۲۰۷ دستی» نقدشوندگی ۹۰. سگمنت‌بندی بازار هم روی همین مدل انجام می‌شود.
/// </summary>
public class VehicleModel
{
    public int Id { get; set; }

    /// <summary>برند، مثل «پژو».</summary>
    public string Brand { get; set; } = string.Empty;

    /// <summary>مدل، مثل «۲۰۷».</summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>تریم/تیپ، مثل «دستی پانوراما»؛ ممکن است خالی باشد.</summary>
    public string? Trim { get; set; }

    /// <summary>
    /// شناسه‌ی قطعیِ مدل در دیوار (brand_model، مثل «Peugeot 206 3-normal»)؛ یکتا.
    /// مبنای نگاشتِ قطعی و رجیستر خودکار؛ NULL برای مدل‌های seedِ دستی.
    /// </summary>
    public string? DivarKey { get; set; }

    /// <summary>نوع بدنه، مثل «هاچ‌بک»/«سدان»/«شاسی‌بلند».</summary>
    public string? BodyType { get; set; }

    /// <summary>نقدشوندگی از پیش‌تعیین‌شده ۰ تا ۱۰۰ (سرعت فروش).</summary>
    public int LiquidityScore { get; set; }

    /// <summary>
    /// نام‌های مستعار برای تطبیق متن آگهی (JSON آرایه‌ی رشته)، مثل «207»، «پژو 207»، «peugeot 207».
    /// </summary>
    public IReadOnlyList<string> Aliases { get; set; } = Array.Empty<string>();

    /// <summary>آیا رنگ برای این مدل همیشه در قیمت مهم است (مستقل از آستانه‌ی ۴ میلیارد).</summary>
    public bool ColorSensitive { get; set; }

    /// <summary>آیا این مدل فعال است (در نرمال‌سازی/سگمنت استفاده شود).</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>یادداشت.</summary>
    public string? Notes { get; set; }
}
