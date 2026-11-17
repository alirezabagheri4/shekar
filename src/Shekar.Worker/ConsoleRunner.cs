using Microsoft.Extensions.DependencyInjection;
using Shekar.Worker.Jobs;

namespace Shekar.Worker;

/// <summary>
/// حالتِ «کنسولِ دستی» برای توسعه روی لوکال: به‌جای زمان‌بندِ Quartz، یک منوی ساده نشان می‌دهد و
/// با انتخابِ هر گزینه، همان مرحله‌ی خط لوله را یک‌بار و به‌صورتِ کامل اجرا می‌کند. هر سه جاب
/// همان منطقِ زمان‌بندی‌شده را از طریقِ <c>RunOnceAsync</c> به‌اشتراک می‌گذارند.
/// </summary>
public static class ConsoleRunner
{
    public static async Task RunAsync(IServiceProvider services, CancellationToken appStopping)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        while (!appStopping.IsCancellationRequested)
        {
            Console.WriteLine();
            Console.WriteLine("════════════ شکار · کنسولِ دستی (دیباگ) ════════════");
            Console.WriteLine("  1) کراول دیتا        — دریافت و ذخیره‌ی آگهی‌ها");
            Console.WriteLine("  2) اگریگیشن          — نرمال‌سازی + ساخت سگمنت‌های بازار");
            Console.WriteLine("  3) اسکورینگ          — ساخت/به‌روزرسانیِ شکارها");
            Console.WriteLine("  ا) اجرای کاملِ خط لوله (۱ ← ۲ ← ۳)");
            Console.WriteLine("  0) خروج");
            Console.Write("انتخاب: ");

            string? choice = Console.ReadLine()?.Trim();
            if (choice is null || choice is "0" or "q" or "خروج")
            {
                break;
            }

            try
            {
                switch (choice)
                {
                    case "1":
                        await RunStepAsync(services, "کراول", appStopping, static (sp, ct) =>
                            ActivatorUtilities.CreateInstance<CrawlJob>(sp).RunOnceAsync(ct));
                        break;
                    case "2":
                        await RunStepAsync(services, "اگریگیشن", appStopping, static (sp, ct) =>
                            ActivatorUtilities.CreateInstance<AggregateJob>(sp).RunOnceAsync(ct));
                        break;
                    case "3":
                        await RunStepAsync(services, "اسکورینگ", appStopping, static (sp, ct) =>
                            ActivatorUtilities.CreateInstance<ScoreJob>(sp).RunOnceAsync(ct));
                        break;
                    case "ا" or "a" or "A" or "4":
                        await RunStepAsync(services, "کراول", appStopping, static (sp, ct) =>
                            ActivatorUtilities.CreateInstance<CrawlJob>(sp).RunOnceAsync(ct));
                        await RunStepAsync(services, "اگریگیشن", appStopping, static (sp, ct) =>
                            ActivatorUtilities.CreateInstance<AggregateJob>(sp).RunOnceAsync(ct));
                        await RunStepAsync(services, "اسکورینگ", appStopping, static (sp, ct) =>
                            ActivatorUtilities.CreateInstance<ScoreJob>(sp).RunOnceAsync(ct));
                        break;
                    default:
                        Console.WriteLine("گزینه‌ی نامعتبر.");
                        break;
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"خطا: {ex.Message}");
                Console.ResetColor();
            }
        }

        Console.WriteLine("خداحافظ.");
    }

    private static async Task RunStepAsync(
        IServiceProvider services, string label, CancellationToken ct,
        Func<IServiceProvider, CancellationToken, Task> run)
    {
        Console.WriteLine($"▶ شروعِ {label}…");
        var sw = System.Diagnostics.Stopwatch.StartNew();
        await run(services, ct);
        sw.Stop();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"✓ {label} تمام شد ({sw.Elapsed.TotalSeconds:0.0} ثانیه). جزئیات در لاگ‌های بالا.");
        Console.ResetColor();
    }
}
