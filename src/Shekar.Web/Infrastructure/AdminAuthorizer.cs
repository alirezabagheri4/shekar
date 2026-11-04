using Shekar.Application.Abstractions;

namespace Shekar.Web.Infrastructure;

/// <summary>
/// تشخیص ادمین‌بودن کاربر جاری. فهرست شماره‌های ادمین از پیکربندی «Admin:Mobiles» خوانده می‌شود
/// (هیچ نقشی هاردکد نیست). کاربر لاگین‌شده‌ای که شماره‌اش در این فهرست باشد ادمین است.
/// </summary>
public interface IAdminAuthorizer
{
    Task<bool> IsAdminAsync(CancellationToken ct = default);
}

public sealed class AdminAuthorizer : IAdminAuthorizer
{
    private readonly ICurrentUser _currentUser;
    private readonly IUserRepository _users;
    private readonly HashSet<string> _adminMobiles;

    public AdminAuthorizer(ICurrentUser currentUser, IUserRepository users, IConfiguration config)
    {
        _currentUser = currentUser;
        _users = users;
        _adminMobiles = config.GetSection("Admin:Mobiles").Get<string[]>()
            ?.Select(m => m.Trim()).Where(m => m.Length > 0).ToHashSet()
            ?? new HashSet<string>();
    }

    public async Task<bool> IsAdminAsync(CancellationToken ct = default)
    {
        if (_adminMobiles.Count == 0 || !_currentUser.IsAuthenticated)
        {
            return false;
        }

        var user = await _users.GetAsync(_currentUser.RequireUserId(), ct);
        return user is not null && _adminMobiles.Contains(user.Mobile);
    }
}
