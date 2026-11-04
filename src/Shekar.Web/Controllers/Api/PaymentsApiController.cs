using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shekar.Application.Abstractions;
using Shekar.Application.Common;
using Shekar.Application.Contracts;
using Shekar.Application.Services;
using Shekar.Domain.Enums;

namespace Shekar.Web.Controllers.Api;

[ApiController]
[Route("api/payments")]
[Authorize]
public sealed class PaymentsApiController : ControllerBase
{
    private const long MaxFileBytes = 5 * 1024 * 1024;
    private static readonly string[] AllowedContentTypes = { "image/jpeg", "image/png", "image/webp" };

    private readonly IPaymentService _payments;
    private readonly IReceiptStorage _storage;
    private readonly ILogger<PaymentsApiController> _logger;

    public PaymentsApiController(
        IPaymentService payments, IReceiptStorage storage, ILogger<PaymentsApiController> logger)
    {
        _payments = payments;
        _storage = storage;
        _logger = logger;
    }

    /// <summary>ثبت پرداخت کارت‌به‌کارت (multipart): plan, method, receiptImage یا receiptText.</summary>
    [HttpPost("card-transfer")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> CardTransfer(
        [FromForm] string plan,
        [FromForm] string method,
        IFormFile? receiptImage,
        [FromForm] string? receiptText,
        CancellationToken ct)
    {
        PlanType planType = string.Equals(plan, "pro", StringComparison.OrdinalIgnoreCase)
            ? PlanType.HunterPro
            : PlanType.Hunter;

        PaymentReceiptMethod receiptMethod = string.Equals(method, "text", StringComparison.OrdinalIgnoreCase)
            ? PaymentReceiptMethod.Text
            : PaymentReceiptMethod.Image;

        string? imageKey = null;
        if (receiptMethod == PaymentReceiptMethod.Image)
        {
            if (receiptImage is null || receiptImage.Length == 0)
            {
                throw new AppValidationException("عکس رسید رو بفرست.");
            }

            if (receiptImage.Length > MaxFileBytes)
            {
                throw new AppValidationException("حجم عکس بیشتر از ۵ مگابایته.");
            }

            if (!AllowedContentTypes.Contains(receiptImage.ContentType))
            {
                throw new AppValidationException("فقط عکس (JPG، PNG، WEBP) قبوله.");
            }

            string ext = Path.GetExtension(receiptImage.FileName);
            await using Stream upload = receiptImage.OpenReadStream();
            imageKey = await _storage.SaveAsync(upload, ext, ct);
        }

        var result = await _payments.SubmitCardTransferAsync(new CardTransferSubmissionDto
        {
            PlanType = planType,
            Method = receiptMethod,
            ReceiptImageKey = imageKey,
            ReceiptText = receiptText,
            Ip = HttpContext.Connection.RemoteIpAddress?.ToString()
        }, ct);

        _logger.LogInformation(
            "پرداخت کارت‌به‌کارت ثبت شد: #{PaymentId} ({Reference})، پلن {Plan}",
            result.PaymentId, result.Reference, planType);

        return Ok(new { paymentId = result.PaymentId, reference = result.Reference });
    }
}
