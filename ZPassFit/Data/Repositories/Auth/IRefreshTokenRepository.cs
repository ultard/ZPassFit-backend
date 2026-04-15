using ZPassFit.Data.Models;

namespace ZPassFit.Data.Repositories.Auth;

public interface IRefreshTokenRepository
{
    Task AddAndSaveAsync(RefreshToken token, CancellationToken cancellationToken = default);

    Task<RefreshToken?> FindActiveByHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task RevokeAsync(RefreshToken token, CancellationToken cancellationToken = default);

    Task<bool> TryRevokeAsync(string tokenHash, string userId, CancellationToken cancellationToken = default);

    Task RevokeAllForUserAsync(string userId, CancellationToken cancellationToken = default);
}
