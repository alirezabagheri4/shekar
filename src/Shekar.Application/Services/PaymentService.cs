using Shekar.Application.Abstractions;
using Shekar.Application.Common;
using Shekar.Application.Contracts;
using Shekar.Domain.Entities;
using Shekar.Domain.Enums;

namespace Shekar.Application.Services;

/// <summary>
/// پرداخت کارت‌به‌کارت: ثبت واریزی و فعال‌سازی/تمدید اشتراک (منتظر تأیید) و بررسی ادمین.
/// فعال‌سازی ظرفیت شهر (R1) را قبل از گرفتن یک جای تازه رعایت می‌کند.
/// </summary>
public sealed class PaymentService : IPaymentService
{
    // کلیدهای تنظیمات سراسری کارت مقصد (قابل تغییر بدون انتشار مجدد).
    private const string CardNumberKey = "CardTransfer.CardNumber";
    private const string HolderNameKey = "CardTransfer.HolderName";
    private const string BankNameKey = "CardTransfer.BankName";

    private const string DefaultCardNumber = "6219861082608372";
    private const string DefaultHolderName = "علیرضا باقری";
    private const string DefaultBankName = "بانک سامان";

    private const int MinReceiptTextLength = 20;

    private readonly IPaymentRepository _payments;
    private readonly ISubscriptionRepository _subscriptions;
    private readonly IUserRepository _users;
    private readonly ICityRepository _cities;
    private readonly IPlanRepository _plans;
    private readonly IBlockedAdvertiserRepository _blocked;
    private readonly ISystemSettingRepository _settings;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;
    private readonly IJalaliCalendar _jalali;

    public PaymentService(
        IPaymentRepository payments,
        ISubscriptionRepository subscriptions,
        IUserRepository users,
        ICityRepository cities,
        IPlanRepository plans,
        IBlockedAdvertiserRepository blocked,
        ISystemSettingRepository settings,
        ICurrentUser currentUser,
        IClock clock,
        IJalaliCalendar jalali)
    {
        _payments = payments;
        _subscriptions = subscriptions;
        _users = users;
        _cities = cities;
        _plans = plans;
        _blocked = blocked;
        _settings = settings;
        _currentUser = currentUser;
        _clock = clock;
        _jalali = jalali;
    }

    public async Task<CardTransferInfoDto> GetCardInfoAsync(CancellationToken ct = default)
    {
        string card = await GetSettingAsync(CardNumberKey, DefaultCardNumber, ct);
        string holder = await GetSettingAsync(HolderNameKey, DefaultHolderName, ct);
        string bank = await GetSettingAsync(BankNameKey, DefaultBankName, ct);

        return new CardTransferInfoDto
        {
            CardNumber = card,
            CardNumberGrouped = GroupCardNumber(card),
            HolderName = holder,
            BankName = bank
        };
    }

    public async Task<CardTransferResultDto> SubmitCardTransferAsync(
        CardTransferSubmissionDto request, CancellationToken ct = default)
    {
        int userId = _currentUser.RequireUserId();
        User user = await _users.GetAsync(userId, ct) ?? throw new NotAuthenticatedException();

        DateTimeOffset now = _clock.UtcNow;

        // شماره‌ی بلاک‌شده نمی‌تواند پرداخت ثبت کند.
        if (await _blocked.IsBlockedAsync(user.Mobile, now, ct))
        {
            throw new ForbiddenException(ErrorCodes.AccountBlocked, BlockedMessage);
        }

        // هر کاربر در هر لحظه حداکثر یک پرداخت «در انتظار بررسی».
        if (await _payments.GetPendingForUserAsync(userId, ct) is not null)
        {
            throw new ConflictException(
                ErrorCodes.PaymentPending,
                "یه پرداخت در انتظار بررسی داری؛ تا نتیجه‌ش مشخص نشده نمی‌تونی پرداخت تازه ثبت کنی.");
        }

        // اعتبارسنجی رسید.
        if (request.Method == PaymentReceiptMethod.Image)
        {
            if (string.IsNullOrWhiteSpace(request.ReceiptImageKey))
            {
                throw new AppValidationException("عکس رسید رو بفرست.");
            }
        }
        else if (string.IsNullOrWhiteSpace(request.ReceiptText)
                 || request.ReceiptText.Trim().Length < MinReceiptTextLength)
        {
            throw new AppValidationException("متن رسید کوتاهه؛ مبلغ، تاریخ و شماره پیگیری رو کامل بچسبون.");
        }

        Plan plan = await _plans.GetAsync(request.PlanType, ct)
            ?? throw new NotFoundException("پلن پیدا نشد.");

        int cityId = user.CityId ?? TehranCityId;

        // فعال‌سازی/تمدید اشتراک.
        Subscription? active = await _subscriptions.GetActiveForUserAsync(userId, now, ct);
        Subscription subscription;
        if (active is not null)
        {
            // اشتراک فعال دارد → یک ماه به انتهای اشتراک فعلی اضافه می‌شود (جای تازه اشغال نمی‌شود).
            active.ExpiresAt = active.ExpiresAt.AddMonths(1);
            active.PlanType = request.PlanType;
            await _subscriptions.UpdateAsync(active, ct);
            subscription = active;
        }
        else
        {
            // جای تازه → ابتدا ظرفیت شهر (R1) چک می‌شود.
            await EnsureCapacityAsync(cityId, now, ct);
            subscription = await _subscriptions.AddAsync(new Subscription
            {
                UserId = userId,
                CityId = cityId,
                PlanType = request.PlanType,
                Status = SubscriptionStatus.Active,
                StartsAt = now,
                ExpiresAt = now.AddMonths(1)
            }, ct);
        }

        string reference = GenerateReference();
        Payment payment = await _payments.AddAsync(new Payment
        {
            UserId = userId,
            Mobile = user.Mobile,
            PlanType = request.PlanType,
            CityId = cityId,
            AmountToman = plan.MonthlyPriceToman,
            Method = request.Method,
            ReceiptImageKey = request.Method == PaymentReceiptMethod.Image ? request.ReceiptImageKey : null,
            ReceiptText = request.Method == PaymentReceiptMethod.Text ? request.ReceiptText!.Trim() : null,
            Reference = reference,
            Ip = request.Ip,
            SubmittedAt = now,
            Status = PaymentReviewStatus.Pending,
            SubscriptionId = subscription.Id
        }, ct);

        return new CardTransferResultDto
        {
            PaymentId = payment.Id,
            Reference = reference,
            PlanType = request.PlanType,
            ExpiresLabel = _jalali.FormatDate(ToTehran(subscription.ExpiresAt))
        };
    }

    public async Task<string> GetActiveExpiresLabelAsync(CancellationToken ct = default)
    {
        int userId = _currentUser.RequireUserId();
        Subscription? active = await _subscriptions.GetActiveForUserAsync(userId, _clock.UtcNow, ct);
        return active is null ? string.Empty : _jalali.FormatDate(ToTehran(active.ExpiresAt));
    }

    public async Task<IReadOnlyList<PendingPaymentDto>> ListPendingAsync(CancellationToken ct = default)
    {
        IReadOnlyList<Payment> pending = await _payments.ListPendingAsync(ct);
        var result = new List<PendingPaymentDto>(pending.Count);
        foreach (Payment p in pending)
        {
            User? user = await _users.GetAsync(p.UserId, ct);
            string fullName = user is null
                ? "—"
                : $"{user.FirstName} {user.LastName}".Trim();

            result.Add(new PendingPaymentDto
            {
                Id = p.Id,
                Reference = p.Reference,
                Mobile = p.Mobile,
                FullName = string.IsNullOrWhiteSpace(fullName) ? "—" : fullName,
                PlanName = p.PlanType == PlanType.HunterPro ? "شکارچی حرفه‌ای" : "شکارچی",
                AmountToman = p.AmountToman,
                AmountLabel = PersianFormat.ToPersianNumber(p.AmountToman),
                Method = p.Method,
                HasImage = p.Method == PaymentReceiptMethod.Image && !string.IsNullOrEmpty(p.ReceiptImageKey),
                ReceiptText = p.ReceiptText,
                SubmittedAtLabel = _jalali.FormatDate(ToTehran(p.SubmittedAt)),
                Ip = p.Ip
            });
        }

        return result;
    }

    public async Task ApproveAsync(int paymentId, int adminUserId, CancellationToken ct = default)
    {
        Payment payment = await GetPendingAsync(paymentId, ct);

        payment.Status = PaymentReviewStatus.Approved;
        payment.ReviewedAt = _clock.UtcNow;
        payment.ReviewedByUserId = adminUserId;
        payment.ReviewNote = "تأیید واریزی";
        await _payments.UpdateAsync(payment, ct);
        // اشتراک از لحظه‌ی ثبت فعال بوده؛ تأیید فقط وضعیت پرداخت را نهایی می‌کند.
    }

    public async Task RejectAsync(int paymentId, int adminUserId, CancellationToken ct = default)
    {
        Payment payment = await GetPendingAsync(paymentId, ct);
        DateTimeOffset now = _clock.UtcNow;

        payment.Status = PaymentReviewStatus.Rejected;
        payment.ReviewedAt = now;
        payment.ReviewedByUserId = adminUserId;
        payment.ReviewNote = "رسید جعلی";
        await _payments.UpdateAsync(payment, ct);

        // اشتراک فعال‌شده با این پرداخت را همان لحظه غیرفعال کن.
        if (payment.SubscriptionId is { } subId
            && await _subscriptions.GetAsync(subId, ct) is { } subscription)
        {
            subscription.Status = SubscriptionStatus.Expired;
            subscription.ExpiresAt = now;
            await _subscriptions.UpdateAsync(subscription, ct);
        }

        // شماره موبایل را با دلیل و زمان به لیست بلاک اضافه کن (اگر از قبل بلاک فعال ندارد).
        if (!await _blocked.IsBlockedAsync(payment.Mobile, now, ct))
        {
            await _blocked.AddAsync(new BlockedAdvertiser
            {
                Phone = payment.Mobile,
                Reason = BlockReason.FakeReceipt,
                Note = $"رسید جعلی در پرداخت {payment.Reference}",
                BlockedAt = now,
                BlockedByUserId = adminUserId,
                ExpiresAt = null,
                IsActive = true
            }, ct);
        }
    }

    public async Task<IReadOnlyList<BlockedPhoneDto>> ListBlockedAsync(CancellationToken ct = default)
    {
        IReadOnlyList<BlockedAdvertiser> items = await _blocked.ListAsync(ct);
        return items.Select(b => new BlockedPhoneDto
        {
            Id = b.Id,
            Phone = b.Phone,
            ReasonLabel = ReasonLabel(b.Reason),
            Note = b.Note,
            BlockedAtLabel = _jalali.FormatDate(ToTehran(b.BlockedAt)),
            IsActive = b.IsActive && (b.ExpiresAt == null || b.ExpiresAt > _clock.UtcNow)
        }).ToList();
    }

    public async Task UnblockAsync(int blockedId, int adminUserId, CancellationToken ct = default)
    {
        BlockedAdvertiser? entry = await _blocked.GetAsync(blockedId, ct)
            ?? throw new NotFoundException("رکورد بلاک پیدا نشد.");

        entry.IsActive = false;
        await _blocked.UpdateAsync(entry, ct);
    }

    private const int TehranCityId = 1;

    private const string BlockedMessage =
        "حساب شما موقتاً مسدود شده. اگه فکر می‌کنی اشتباهیه، با پشتیبانی تماس بگیر: @shekarai";

    private async Task<Payment> GetPendingAsync(int paymentId, CancellationToken ct)
    {
        Payment payment = await _payments.GetAsync(paymentId, ct)
            ?? throw new NotFoundException("پرداخت پیدا نشد.");

        if (payment.Status != PaymentReviewStatus.Pending)
        {
            throw new ConflictException(ErrorCodes.PaymentPending, "این پرداخت قبلاً بررسی شده.");
        }

        return payment;
    }

    private async Task EnsureCapacityAsync(int cityId, DateTimeOffset now, CancellationToken ct)
    {
        City city = await _cities.GetAsync(cityId, ct)
            ?? throw new NotFoundException("شهر پیدا نشد.");

        int used = await _subscriptions.CountActiveInCityAsync(cityId, now, ct);
        if (used >= city.Capacity)
        {
            throw new ConflictException(
                ErrorCodes.CityCapacityFull,
                $"ظرفیت {city.Name} تکمیل شده؛ تو لیست انتظار ثبت‌نام کن.");
        }
    }

    private async Task<string> GetSettingAsync(string key, string fallback, CancellationToken ct)
    {
        SystemSetting? setting = await _settings.GetAsync(key, ct);
        return string.IsNullOrWhiteSpace(setting?.Value) ? fallback : setting!.Value.Trim();
    }

    private DateTimeOffset ToTehran(DateTimeOffset utc) => utc.ToOffset(_clock.TehranNow.Offset);

    private static string GenerateReference() =>
        "SHK-" + PersianFormat.ToPersianDigits(Random.Shared.Next(100_000, 999_999).ToString());

    private static string GroupCardNumber(string card)
    {
        string digits = new string(card.Where(char.IsDigit).ToArray());
        var groups = new List<string>();
        for (int i = 0; i < digits.Length; i += 4)
        {
            groups.Add(digits.Substring(i, Math.Min(4, digits.Length - i)));
        }

        return PersianFormat.ToPersianDigits(string.Join(' ', groups));
    }

    private static string ReasonLabel(BlockReason reason) => reason switch
    {
        BlockReason.TooManyAds => "آگهی بیش از حد",
        BlockReason.Broker => "دلال",
        BlockReason.FalseInformation => "اطلاعات نادرست",
        BlockReason.Duplicate => "آگهی تکراری",
        BlockReason.Reported => "گزارش کاربران",
        BlockReason.Manual => "بلاک دستی",
        BlockReason.FakeReceipt => "رسید جعلی",
        _ => "نامشخص"
    };
}
