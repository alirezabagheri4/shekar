using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shekar.Application.Contracts;
using Shekar.Application.Services;
using Shekar.Web.Models;

namespace Shekar.Web.Controllers;

[Authorize]
public class PayController : Controller
{
    private readonly IPaymentService _payments;

    public PayController(IPaymentService payments) => _payments = payments;

    // صفحه‌ی پرداخت کارت‌به‌کارت (plan = pro|basic).
    [HttpGet("/pay")]
    public async Task<IActionResult> Index(string? plan, CancellationToken ct)
    {
        string selected = string.Equals(plan, "basic", StringComparison.OrdinalIgnoreCase) ? "basic" : "pro";
        CardTransferInfoDto card = await _payments.GetCardInfoAsync(ct);
        return View(new PayViewModel(card, selected));
    }

    // صفحه‌ی بررسی واریزی (۱۵ ثانیه نمایشی، سپس «اشتراکت فعال شد»).
    [HttpGet("/pay/review")]
    public async Task<IActionResult> Review(string? plan, string? @ref, CancellationToken ct)
    {
        string selected = string.Equals(plan, "basic", StringComparison.OrdinalIgnoreCase) ? "basic" : "pro";
        string expires = await _payments.GetActiveExpiresLabelAsync(ct);
        return View(new PayReviewViewModel(selected, @ref ?? string.Empty, expires));
    }

    // صفحه‌ی موفقیت اشتراک آزمایشی (R3) — جریان ترایل همچنان از این مسیر استفاده می‌کند.
    [HttpGet("/pay/success")]
    public IActionResult Success()
    {
        var vm = new PaySuccessViewModel(
            PlanName: TempData["PayPlan"] as string ?? "شکارچی حرفه‌ای",
            Amount: TempData["PayAmount"] as string ?? string.Empty,
            ExpiresLabel: TempData["PayExpires"] as string ?? string.Empty,
            TrackingCode: TempData["PayTracking"] as string ?? string.Empty,
            IsTrial: (TempData["PayTrial"] as string) == "1");
        return View(vm);
    }
}
