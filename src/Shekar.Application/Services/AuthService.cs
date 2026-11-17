using Shekar.Application.Abstractions;
using Shekar.Application.Common;
using Shekar.Application.Contracts;
using Shekar.Domain.Entities;

namespace Shekar.Application.Services;

/// <summary>تنظیماتِ ورود.</summary>
public sealed record AuthOptions
{
    /// <summary>
    /// کدِ ورودِ «استاد» برای راه‌اندازی/تست: اگر تنظیم شود، <b>هر</b> شماره‌ی موبایلِ معتبر با همین کد
    /// وارد می‌شود، بدونِ نیاز به دریافتِ پیامک و بدونِ انقضا. خالی/null = غیرفعال (مسیرِ عادیِ OTP).
    /// ⚠️ در تولیدِ واقعی باید خالی شود؛ وگرنه هرکسی می‌تواند به‌جای هرکسی وارد شود.
    /// </summary>
    public string? MasterOtpCode { get; init; }
}

/// <summary>ورود با شماره موبایل + OTP پنج‌رقمی و تکمیل پروفایل در ورود اول.</summary>
public sealed class AuthService : IAuthService
{
    private static readonly TimeSpan OtpLifetime = TimeSpan.FromMinutes(2);

    private readonly IOtpRepository _otps;
    private readonly IUserRepository _users;
    private readonly IOtpGenerator _otpGenerator;
    private readonly ISmsSender _sms;
    private readonly IClock _clock;
    private readonly IBlockedAdvertiserRepository _blocked;
    private readonly AuthOptions _options;

    public AuthService(
        IOtpRepository otps,
        IUserRepository users,
        IOtpGenerator otpGenerator,
        ISmsSender sms,
        IClock clock,
        IBlockedAdvertiserRepository blocked,
        AuthOptions options)
    {
        _otps = otps;
        _users = users;
        _otpGenerator = otpGenerator;
        _sms = sms;
        _clock = clock;
        _blocked = blocked;
        _options = options;
    }

    /// <summary>پیام محترمانه‌ی حساب مسدودشده با لینک پشتیبانی.</summary>
    private const string BlockedMessage =
        "حساب شما موقتاً مسدود شده. اگه فکر می‌کنی اشتباهیه، با پشتیبانی تماس بگیر: @shekarai";

    public async Task RequestOtpAsync(RequestOtpDto request, CancellationToken ct = default)
    {
        DateTimeOffset now = _clock.UtcNow;

        // شماره‌ی بلاک‌شده نمی‌تواند کد ورود بگیرد.
        if (await _blocked.IsBlockedAsync(request.Mobile, now, ct))
        {
            throw new ForbiddenException(ErrorCodes.AccountBlocked, BlockedMessage);
        }

        string code = _otpGenerator.Generate();

        await _otps.AddAsync(new OtpCode
        {
            Mobile = request.Mobile,
            Code = code,
            CreatedAt = now,
            ExpiresAt = now + OtpLifetime,
            Consumed = false
        }, ct);

        await _sms.SendAsync(request.Mobile, $"کد ورود شکار: {code}", ct);
    }

    public async Task<AuthResultDto> VerifyOtpAsync(VerifyOtpDto request, CancellationToken ct = default)
    {
        DateTimeOffset now = _clock.UtcNow;

        // شماره‌ی بلاک‌شده نمی‌تواند وارد شود.
        if (await _blocked.IsBlockedAsync(request.Mobile, now, ct))
        {
            throw new ForbiddenException(ErrorCodes.AccountBlocked, BlockedMessage);
        }

        // کدِ استاد (راه‌اندازی/تست): هر شماره با این کد وارد می‌شود، بدونِ پیامک و بدونِ انقضا.
        bool masterMatch = !string.IsNullOrEmpty(_options.MasterOtpCode)
            && string.Equals(request.Code, _options.MasterOtpCode, StringComparison.Ordinal);

        if (!masterMatch)
        {
            OtpCode? otp = await _otps.GetLatestAsync(request.Mobile, ct);

            if (otp is null || otp.Consumed || otp.ExpiresAt <= now || otp.Code != request.Code)
            {
                throw new AppValidationException("کد درست نیست یا منقضی شده.");
            }

            otp.Consumed = true;
            await _otps.UpdateAsync(otp, ct);
        }

        User? user = await _users.GetByMobileAsync(request.Mobile, ct);
        if (user is null)
        {
            user = await _users.AddAsync(new User
            {
                Mobile = request.Mobile,
                CreatedAt = now,
                HasUsedTrial = false,
                ProfileCompleted = false
            }, ct);
        }

        return new AuthResultDto
        {
            UserId = user.Id,
            RequiresProfile = !user.ProfileCompleted
        };
    }

    public async Task CompleteProfileAsync(int userId, CompleteProfileDto request, CancellationToken ct = default)
    {
        User user = await _users.GetAsync(userId, ct)
            ?? throw new NotAuthenticatedException();

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.CityId = request.CityId;
        user.ProfileCompleted = true;

        await _users.UpdateAsync(user, ct);
    }
}
