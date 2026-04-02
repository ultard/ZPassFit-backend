using ZPassFit.Data.Models;

namespace ZPassFit.Services.Interfaces;

public interface IJwtTokenService
{
    Task<AuthTokenPair> CreateTokensAsync(ApplicationUser user, CancellationToken cancellationToken = default);

    Task<AuthTokenPair?> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);

    Task RevokeRefreshTokenAsync(string refreshToken, string userId, CancellationToken cancellationToken = default);

    Task RevokeAllRefreshTokensAsync(string userId, CancellationToken cancellationToken = default);
}

public record AuthTokenPair(string AccessToken, string RefreshToken, DateTime AccessTokenExpiresAtUtc);
