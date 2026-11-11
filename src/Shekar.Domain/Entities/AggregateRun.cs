using Shekar.Domain.Enums;

namespace Shekar.Domain.Entities;

/// <summary>لاگ هر پاس اگریگیشن (نرمال‌سازی + ساخت سگمنت)؛ برای پایش سلامت و عیب‌یابیِ داده‌سازی.</summary>
public class AggregateRun
{
    public long Id { get; set; }

    public DateTimeOffset StartedAt { get; set; }

    public DateTimeOffset? FinishedAt { get; set; }

    /// <summary>وضعیت: 1=در حال اجرا، 2=موفق، 3=خطا (هم‌معنا با CrawlStatus).</summary>
    public CrawlStatus Status { get; set; } = CrawlStatus.Running;

    /// <summary>تعداد آگهیِ نرمال‌شده در این پاس.</summary>
    public int NormalizedCount { get; set; }

    /// <summary>تعداد مدلِ رجیسترشده‌ی خودکار در این پاس.</summary>
    public int AutoRegistered { get; set; }

    /// <summary>تعداد آگهیِ بی‌تطبیق‌مانده.</summary>
    public int UnmatchedCount { get; set; }

    /// <summary>تعداد کلِ سگمنت‌های ساخته/به‌روزشده.</summary>
    public int SegmentCount { get; set; }

    /// <summary>تعداد سگمنت‌های «قابل‌اتکا» (نمونه‌ی کافی + پراکندگیِ محدود).</summary>
    public int ReliableSegmentCount { get; set; }

    /// <summary>تعداد سگمنت‌هایی که این پاس کهنه (بی‌اعتبار) شدند.</summary>
    public int StaledCount { get; set; }

    /// <summary>پیام خطا در صورت شکست.</summary>
    public string? Error { get; set; }
}
