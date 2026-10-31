using System.Text;

namespace Shekar.Application.Common;

/// <summary>نرمال‌سازی متن فارسی برای تطبیق نام خودرو (ارقام، ی/ک عربی، نیم‌فاصله، فاصله).</summary>
public static class TextNormalizer
{
    /// <summary>متن را به شکل یکدست برای مقایسه برمی‌گرداند (ارقام لاتین، بدون نیم‌فاصله، فاصله‌ی تک، lowercase).</summary>
    public static string Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var sb = new StringBuilder(input.Length);
        foreach (char ch in input)
        {
            char c = ch switch
            {
                >= '۰' and <= '۹' => (char)('0' + (ch - '۰')),   // ارقام فارسی
                >= '٠' and <= '٩' => (char)('0' + (ch - '٠')),   // ارقام عربی
                'ي' => 'ی',
                'ك' => 'ک',
                'ۀ' => 'ه',
                'إ' or 'أ' or 'آ' => 'ا',
                '‌' or '‏' or '‎' => ' ',          // نیم‌فاصله و کنترل جهت
                _ => char.ToLowerInvariant(ch)
            };
            sb.Append(c);
        }

        // فشرده‌سازی فاصله‌ها.
        return string.Join(' ', sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }
}
