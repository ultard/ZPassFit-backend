using ZPassFit.Data.Models;

namespace ZPassFit.Services.Interfaces;

/// <summary>
/// Роли и загрузка пользователя для JWT (обёртка над Identity UserManager).
/// </summary>
public interface IApplicationUserIdentityService
{
    Task<IReadOnlyList<string>> GetRolesAsync(ApplicationUser user, CancellationToken cancellationToken = default);

    Task<ApplicationUser?> FindByIdAsync(string userId, CancellationToken cancellationToken = default);
}
