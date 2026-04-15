using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ZPassFit.Auth;
using ZPassFit.Data.Models;
using ZPassFit.Data.Repositories.Auth;
using ZPassFit.Services.Interfaces;

namespace ZPassFit.Services.Implementations;

public class JwtTokenService(
    IRefreshTokenRepository refreshTokens,
    IApplicationUserIdentityService users,
    IOptions<JwtOptions> options
) : IJwtTokenService
{
    private readonly JwtOptions _jwt = options.Value;
    private const string TokenTypeClaim = "token_type";
    private const string RefreshTokenType = "refresh";

    public async Task<AuthTokenPair> CreateTokensAsync(
        ApplicationUser user,
        CancellationToken cancellationToken = default
    )
    {
        var (accessToken, expiresAtUtc) = await CreateAccessTokenAsync(user, cancellationToken);
        var (refreshToken, refreshExpiresAtUtc) = CreateRefreshToken(user);
        var hash = HashToken(refreshToken);

        await refreshTokens.AddAndSaveAsync(
            new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TokenHash = hash,
                ExpiresAt = refreshExpiresAtUtc,
                CreatedAt = DateTime.UtcNow
            },
            cancellationToken
        );
        return new AuthTokenPair(accessToken, refreshToken, expiresAtUtc);
    }

    public async Task<AuthTokenPair?> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var principal = ValidateRefreshToken(refreshToken);
        if (principal == null)
            return null;

        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (string.IsNullOrWhiteSpace(userId))
            return null;

        var hash = HashToken(refreshToken);
        var stored = await refreshTokens.FindActiveByHashAsync(hash, cancellationToken);

        if (stored == null)
            return null;

        if (!string.Equals(stored.UserId, userId, StringComparison.Ordinal))
            return null;

        var user = await users.FindByIdAsync(userId, cancellationToken);
        if (user == null)
            return null;

        await refreshTokens.RevokeAsync(stored, cancellationToken);

        return await CreateTokensAsync(user, cancellationToken);
    }

    public async Task RevokeRefreshTokenAsync(
        string refreshToken,
        string userId,
        CancellationToken cancellationToken = default
    )
    {
        var hash = HashToken(refreshToken);
        await refreshTokens.TryRevokeAsync(hash, userId, cancellationToken);
    }

    public Task RevokeAllRefreshTokensAsync(string userId, CancellationToken cancellationToken = default) =>
        refreshTokens.RevokeAllForUserAsync(userId, cancellationToken);

    private async Task<(string Token, DateTime ExpiresAtUtc)> CreateAccessTokenAsync(
        ApplicationUser user,
        CancellationToken cancellationToken
    )
    {
        var roles = await users.GetRolesAsync(user, cancellationToken);
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
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(_jwt.AccessTokenExpirationMinutes);

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        return (tokenString, expires);
    }

    private (string Token, DateTime ExpiresAtUtc) CreateRefreshToken(ApplicationUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(TokenTypeClaim, RefreshTokenType)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddDays(_jwt.RefreshTokenExpirationDays);

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        return (tokenString, expires);
    }

    private ClaimsPrincipal? ValidateRefreshToken(string refreshToken)
    {
        var handler = new JwtSecurityTokenHandler();
        try
        {
            var principal = handler.ValidateToken(
                refreshToken,
                new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidIssuer = _jwt.Issuer,
                    ValidAudience = _jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret)),
                    ClockSkew = TimeSpan.FromSeconds(30)
                },
                out var validatedToken
            );

            if (validatedToken is not JwtSecurityToken jwtToken)
                return null;

            if (!string.Equals(jwtToken.Header.Alg, SecurityAlgorithms.HmacSha256, StringComparison.Ordinal))
                return null;

            var tokenType = principal.FindFirstValue(TokenTypeClaim);
            if (!string.Equals(tokenType, RefreshTokenType, StringComparison.Ordinal))
                return null;

            return principal;
        }
        catch
        {
            return null;
        }
    }

    private static string HashToken(string token)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hash);
    }
}
