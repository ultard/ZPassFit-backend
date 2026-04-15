using Microsoft.EntityFrameworkCore;
using ZPassFit.Data.Models;

namespace ZPassFit.Data.Repositories.Auth;

public class RefreshTokenRepository(ApplicationDbContext context) : IRefreshTokenRepository
{
    public async Task AddAndSaveAsync(RefreshToken token, CancellationToken cancellationToken = default)
    {
        context.RefreshTokens.Add(token);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<RefreshToken?> FindActiveByHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await context.RefreshTokens.FirstOrDefaultAsync(
            t => t.TokenHash == tokenHash && t.RevokedAt == null && t.ExpiresAt > now,
            cancellationToken
        );
    }

    public async Task RevokeAsync(RefreshToken token, CancellationToken cancellationToken = default)
    {
        token.RevokedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> TryRevokeAsync(string tokenHash, string userId, CancellationToken cancellationToken = default)
    {
        var stored = await context.RefreshTokens.FirstOrDefaultAsync(
            t => t.TokenHash == tokenHash && t.UserId == userId && t.RevokedAt == null,
            cancellationToken
        );

        if (stored == null)
            return false;

        stored.RevokedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task RevokeAllForUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        await context.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), cancellationToken);
    }
}
