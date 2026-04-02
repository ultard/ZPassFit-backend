using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ZPassFit.Auth;
using ZPassFit.Data;
using ZPassFit.Data.Models;
using ZPassFit.Services.Interfaces;

namespace ZPassFit.Services.Implementations;

public class JwtTokenService(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    IOptions<JwtOptions> options
) : IJwtTokenService
{
    private readonly JwtOptions _jwt = options.Value;

    public async Task<AuthTokenPair> CreateTokensAsync(
        ApplicationUser user,
        CancellationToken cancellationToken = default
    )
    {
        var (accessToken, expiresAtUtc) = await CreateAccessTokenAsync(user, cancellationToken);
        var refreshPlain = GenerateRefreshTokenPlain();
        var hash = HashToken(refreshPlain);

        db.RefreshTokens.Add(
            new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TokenHash = hash,
                ExpiresAt = DateTime.UtcNow.AddDays(_jwt.RefreshTokenExpirationDays),
                CreatedAt = DateTime.UtcNow
            }
        );

        await db.SaveChangesAsync(cancellationToken);
        return new AuthTokenPair(accessToken, refreshPlain, expiresAtUtc);
    }

    public async Task<AuthTokenPair?> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var hash = HashToken(refreshToken);
        var stored = await db.RefreshTokens.FirstOrDefaultAsync(
            t => t.TokenHash == hash && t.RevokedAt == null && t.ExpiresAt > DateTime.UtcNow,
            cancellationToken
        );

        if (stored == null)
            return null;

        var user = await userManager.FindByIdAsync(stored.UserId);
        if (user == null)
            return null;

        stored.RevokedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return await CreateTokensAsync(user, cancellationToken);
    }

    public async Task RevokeRefreshTokenAsync(
        string refreshToken,
        string userId,
        CancellationToken cancellationToken = default
    )
    {
        var hash = HashToken(refreshToken);
        var stored = await db.RefreshTokens.FirstOrDefaultAsync(
            t => t.TokenHash == hash && t.UserId == userId && t.RevokedAt == null,
            cancellationToken
        );

        if (stored == null)
            return;

        stored.RevokedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeAllRefreshTokensAsync(string userId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        await db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), cancellationToken);
    }

    private async Task<(string Token, DateTime ExpiresAtUtc)> CreateAccessTokenAsync(
        ApplicationUser user,
        CancellationToken cancellationToken
    )
    {
        var roles = await userManager.GetRolesAsync(user);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(_jwt.AccessTokenExpirationMinutes);

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        return (tokenString, expires);
    }

    private static string GenerateRefreshTokenPlain()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }

    private static string HashToken(string token)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hash);
    }
}
