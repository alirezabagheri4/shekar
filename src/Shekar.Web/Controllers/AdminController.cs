using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shekar.Application.Abstractions;
using Shekar.Application.Services;
using Shekar.Web.Infrastructure;
using Shekar.Web.Models;

namespace Shekar.Web.Controllers;

/// <summary>پنل ساده‌ی ادمین: بررسی پرداخت‌های کارت‌به‌کارت و مدیریت شماره‌های بلاک‌شده.</summary>
[Authorize]
[Route("admin")]
public class AdminController : Controller
{
    private readonly IPaymentService _payments;
    private readonly IPaymentRepository _paymentRepo;
    private readonly IReceiptStorage _storage;
    private readonly IAdminAuthorizer _admin;
    private readonly ICurrentUser _currentUser;
    private readonly IPipelineHealthService _health;
    private readonly IConfiguration _config;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IPaymentService payments,
        IPaymentRepository paymentRepo,
        IReceiptStorage storage,
        IAdminAuthorizer admin,
        ICurrentUser currentUser,
        IPipelineHealthService health,
        IConfiguration config,
        ILogger<AdminController> logger)
    {
        _payments = payments;
        _paymentRepo = paymentRepo;
        _storage = storage;
        _admin = admin;
        _currentUser = currentUser;
        _health = health;
        _config = config;
        _logger = logger;
    }

    /// <summary>صفحه‌ی وضعیتِ خط لوله: سلامتِ کراول/نرمال‌سازی/اسکورینگ و هشدارهای عملیاتی.</summary>
    [HttpGet("status")]
    public async Task<IActionResult> Status(CancellationToken ct)
    {
        if (!await _admin.IsAdminAsync(ct))
        {
            return NotFound();
        }

        var options = new PipelineHealthOptions
        {
            StaleCrawlHours = _config.GetValue<int?>("Health:StaleCrawlHours") ?? 6,
            StaleAggregateHours = _config.GetValue<int?>("Health:StaleAggregateHours") ?? 3,
            MinMatchRatePercent = _config.GetValue<int?>("Health:MinMatchRatePercent") ?? 50,
            MinSegmentableOfMatchedPercent = _config.GetValue<int?>("Health:MinSegmentableOfMatchedPercent") ?? 60,
            MinListingsForRate = _config.GetValue<int?>("Health:MinListingsForRate") ?? 20,
            RecentRuns = _config.GetValue<int?>("Health:RecentRuns") ?? 10
        };

        PipelineHealthReport report = await _health.GetAsync(options, ct);
        return View(report);
    }

    [HttpGet("payments")]
    public async Task<IActionResult> Payments(CancellationToken ct)
    {
        if (!await _admin.IsAdminAsync(ct))
        {
            return NotFound();
        }

        var vm = new AdminPaymentsViewModel(
            await _payments.ListPendingAsync(ct),
            await _payments.ListBlockedAsync(ct));
        return View(vm);
    }

    [HttpGet("payments/{id:int}/receipt")]
    public async Task<IActionResult> Receipt(int id, CancellationToken ct)
    {
        if (!await _admin.IsAdminAsync(ct))
        {
            return NotFound();
        }

        var payment = await _paymentRepo.GetAsync(id, ct);
        if (payment?.ReceiptImageKey is not { } key)
        {
            return NotFound();
        }

        ReceiptFile? file = _storage.Open(key);
        return file is null ? NotFound() : File(file.Content, file.ContentType);
    }

    [HttpPost("payments/{id:int}/approve")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id, CancellationToken ct)
    {
        if (!await _admin.IsAdminAsync(ct))
        {
            return NotFound();
        }

        int adminId = _currentUser.RequireUserId();
        await _payments.ApproveAsync(id, adminId, ct);
        _logger.LogInformation("پرداخت #{PaymentId} توسط ادمین {AdminId} تأیید شد.", id, adminId);
        TempData["AdminMsg"] = "پرداخت تأیید شد.";
        return RedirectToAction(nameof(Payments));
    }

    [HttpPost("payments/{id:int}/reject")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, CancellationToken ct)
    {
        if (!await _admin.IsAdminAsync(ct))
        {
            return NotFound();
        }

        int adminId = _currentUser.RequireUserId();
        await _payments.RejectAsync(id, adminId, ct);
        _logger.LogWarning("پرداخت #{PaymentId} توسط ادمین {AdminId} رد شد (رسید جعلی) و شماره بلاک شد.", id, adminId);
        TempData["AdminMsg"] = "پرداخت رد شد؛ اشتراک غیرفعال و شماره بلاک شد.";
        return RedirectToAction(nameof(Payments));
    }

    [HttpPost("blocked/{id:int}/unblock")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unblock(int id, CancellationToken ct)
    {
        if (!await _admin.IsAdminAsync(ct))
        {
            return NotFound();
        }

        int adminId = _currentUser.RequireUserId();
        await _payments.UnblockAsync(id, adminId, ct);
        _logger.LogInformation("شماره‌ی بلاک #{BlockedId} توسط ادمین {AdminId} رفع بلاک شد.", id, adminId);
        TempData["AdminMsg"] = "شماره از بلاک خارج شد.";
        return RedirectToAction(nameof(Payments));
    }
}
