using Microsoft.AspNetCore.Identity;
using ZPassFit.Data.Models;
using ZPassFit.Services.Interfaces;

namespace ZPassFit.Services.Implementations;

public sealed class ApplicationUserIdentityService(UserManager<ApplicationUser> userManager)
    : IApplicationUserIdentityService
{
    public async Task<IReadOnlyList<string>> GetRolesAsync(
        ApplicationUser user,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var roles = await userManager.GetRolesAsync(user);
        return roles.ToList();
    }

    public Task<ApplicationUser?> FindByIdAsync(string userId, CancellationToken cancellationToken = default) =>
        userManager.FindByIdAsync(userId);
}
