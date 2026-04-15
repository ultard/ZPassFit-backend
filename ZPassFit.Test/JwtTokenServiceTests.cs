using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AutoFixture.Xunit3;
using Moq;
using ZPassFit.Data.Models;
using ZPassFit.Data.Repositories.Auth;
using ZPassFit.Services.Implementations;
using ZPassFit.Services.Interfaces;

namespace ZPassFit.Test;

public class JwtTokenServiceTests
{
    private static string HashToken(string token)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hash);
    }

    [Theory]
    [AutoMoqData]
    public async Task CreateTokens_SavesRefreshHash(
        [Frozen] IRefreshTokenRepository refreshTokenRepository,
        [Frozen] IApplicationUserIdentityService identity,
        JwtTokenService jwtTokenService
    )
    {
        var ct = TestContext.Current.CancellationToken;
        var repoMock = Mock.Get(refreshTokenRepository);
        var identityMock = Mock.Get(identity);

        var user = new ApplicationUser { Id = "u1", Email = "a@b.c", UserName = "a@b.c" };
        identityMock.Setup(s => s.GetRolesAsync(user, It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<string>());
        repoMock.Setup(r => r.AddAndSaveAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var pair = await jwtTokenService.CreateTokensAsync(user, ct);

        Assert.False(string.IsNullOrEmpty(pair.AccessToken));
        Assert.False(string.IsNullOrEmpty(pair.RefreshToken));

        repoMock.Verify(
            r =>
                r.AddAndSaveAsync(
                    It.Is<RefreshToken>(
                        t =>
                            t.UserId == user.Id
                            && t.TokenHash == HashToken(pair.RefreshToken)
                            && t.RevokedAt == null
                            && t.ExpiresAt > DateTime.UtcNow
                    ),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
        repoMock.VerifyAll();
        identityMock.VerifyAll();
    }

    [Theory]
    [AutoMoqData]
    public async Task CreateTokens_AccessToken_HasRoles(
        [Frozen] IRefreshTokenRepository refreshTokenRepository,
        [Frozen] IApplicationUserIdentityService identity,
        JwtTokenService jwtTokenService
    )
    {
        var ct = TestContext.Current.CancellationToken;
        var repoMock = Mock.Get(refreshTokenRepository);
        var identityMock = Mock.Get(identity);

        var user = new ApplicationUser { Id = "u-role", Email = "r@b.c", UserName = "r@b.c" };
        identityMock.Setup(s => s.GetRolesAsync(user, It.IsAny<CancellationToken>())).ReturnsAsync(new[] { "Admin", "Coach" });
        repoMock.Setup(r => r.AddAndSaveAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var pair = await jwtTokenService.CreateTokensAsync(user, ct);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(pair.AccessToken);

        Assert.Equal(user.Id, jwt.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
        Assert.Equal(user.Id, jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal(user.Email, jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value);

        var roles = jwt.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).OrderBy(x => x).ToArray();
        Assert.Equal(new[] { "Admin", "Coach" }, roles);

        repoMock.VerifyAll();
        identityMock.VerifyAll();
    }

    [Theory]
    [AutoMoqData]
    public async Task Refresh_NoStored_ReturnsNull(
        [Frozen] IRefreshTokenRepository refreshTokenRepository,
        [Frozen] IApplicationUserIdentityService identity,
        JwtTokenService jwtTokenService
    )
    {
        var ct = TestContext.Current.CancellationToken;
        var repoMock = Mock.Get(refreshTokenRepository);
        var identityMock = Mock.Get(identity);

        var user = new ApplicationUser { Id = "u1", Email = "a@b.c", UserName = "a@b.c" };
        identityMock.Setup(s => s.GetRolesAsync(user, It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<string>());
        repoMock.Setup(r => r.AddAndSaveAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var pair = await jwtTokenService.CreateTokensAsync(user, ct);

        repoMock.Setup(r => r.FindActiveByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        var refreshed = await jwtTokenService.RefreshAsync(pair.RefreshToken, ct);

        Assert.Null(refreshed);
        repoMock.Verify(r => r.RevokeAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Never);
        repoMock.VerifyAll();
        identityMock.VerifyAll();
    }

    [Theory]
    [AutoMoqData]
    public async Task Refresh_UserIdMismatch_ReturnsNull(
        [Frozen] IRefreshTokenRepository refreshTokenRepository,
        [Frozen] IApplicationUserIdentityService identity,
        JwtTokenService jwtTokenService
    )
    {
        var ct = TestContext.Current.CancellationToken;
        var repoMock = Mock.Get(refreshTokenRepository);
        var identityMock = Mock.Get(identity);

        var user = new ApplicationUser { Id = "u1", Email = "a@b.c", UserName = "a@b.c" };
        identityMock.Setup(s => s.GetRolesAsync(user, It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<string>());
        repoMock.Setup(r => r.AddAndSaveAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var pair = await jwtTokenService.CreateTokensAsync(user, ct);

        repoMock.Setup(r => r.FindActiveByHashAsync(HashToken(pair.RefreshToken), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new RefreshToken
                {
                    Id = Guid.NewGuid(),
                    UserId = "other-user",
                    TokenHash = HashToken(pair.RefreshToken),
                    ExpiresAt = DateTime.UtcNow.AddDays(1),
                    CreatedAt = DateTime.UtcNow,
                    RevokedAt = null
                }
            );

        var refreshed = await jwtTokenService.RefreshAsync(pair.RefreshToken, ct);
        Assert.Null(refreshed);
        identityMock.Verify(s => s.FindByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        repoMock.VerifyAll();
        identityMock.VerifyAll();
    }

    [Theory]
    [AutoMoqData]
    public async Task Refresh_UserMissing_ReturnsNull(
        [Frozen] IRefreshTokenRepository refreshTokenRepository,
        [Frozen] IApplicationUserIdentityService identity,
        JwtTokenService jwtTokenService
    )
    {
        var ct = TestContext.Current.CancellationToken;
        var repoMock = Mock.Get(refreshTokenRepository);
        var identityMock = Mock.Get(identity);

        var user = new ApplicationUser { Id = "u1", Email = "a@b.c", UserName = "a@b.c" };
        identityMock.Setup(s => s.GetRolesAsync(user, It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<string>());
        repoMock.Setup(r => r.AddAndSaveAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var pair = await jwtTokenService.CreateTokensAsync(user, ct);

        repoMock.Setup(r => r.FindActiveByHashAsync(HashToken(pair.RefreshToken), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new RefreshToken
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    TokenHash = HashToken(pair.RefreshToken),
                    ExpiresAt = DateTime.UtcNow.AddDays(1),
                    CreatedAt = DateTime.UtcNow,
                    RevokedAt = null
                }
            );

        identityMock.Setup(s => s.FindByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync((ApplicationUser?)null);

        var refreshed = await jwtTokenService.RefreshAsync(pair.RefreshToken, ct);
        Assert.Null(refreshed);
        repoMock.Verify(r => r.RevokeAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Never);
        repoMock.VerifyAll();
        identityMock.VerifyAll();
    }

    [Theory]
    [AutoMoqData]
    public async Task Refresh_Valid_IssuesNewPair(
        [Frozen] IRefreshTokenRepository refreshTokenRepository,
        [Frozen] IApplicationUserIdentityService identity,
        JwtTokenService jwtTokenService
    )
    {
        var ct = TestContext.Current.CancellationToken;
        var repoMock = Mock.Get(refreshTokenRepository);
        var identityMock = Mock.Get(identity);

        var user = new ApplicationUser { Id = "u1", Email = "a@b.c", UserName = "a@b.c" };
        identityMock.Setup(s => s.GetRolesAsync(user, It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<string>());
        repoMock.Setup(r => r.AddAndSaveAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var pair = await jwtTokenService.CreateTokensAsync(user, ct);

        var stored = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = HashToken(pair.RefreshToken),
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow,
            RevokedAt = null
        };

        repoMock.Setup(r => r.FindActiveByHashAsync(HashToken(pair.RefreshToken), It.IsAny<CancellationToken>()))
            .ReturnsAsync(stored);
        identityMock.Setup(s => s.FindByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        repoMock.Setup(r => r.RevokeAsync(stored, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var refreshed = await jwtTokenService.RefreshAsync(pair.RefreshToken, ct);

        Assert.NotNull(refreshed);
        Assert.NotEqual(pair.AccessToken, refreshed!.AccessToken);
        Assert.NotEqual(pair.RefreshToken, refreshed.RefreshToken);

        repoMock.Verify(r => r.RevokeAsync(stored, It.IsAny<CancellationToken>()), Times.Once);
        repoMock.Verify(r => r.AddAndSaveAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        repoMock.VerifyAll();
        identityMock.VerifyAll();
    }

    [Theory]
    [AutoMoqData]
    public async Task RevokeOne_CallsRepository(
        [Frozen] IRefreshTokenRepository refreshTokenRepository,
        [Frozen] IApplicationUserIdentityService identity,
        JwtTokenService jwtTokenService
    )
    {
        var ct = TestContext.Current.CancellationToken;
        const string refresh = "dummy-refresh";
        var expectedHash = HashToken(refresh);

        var repoMock = Mock.Get(refreshTokenRepository);
        repoMock.Setup(r => r.TryRevokeAsync(expectedHash, "uid", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await jwtTokenService.RevokeRefreshTokenAsync(refresh, "uid", ct);

        repoMock.Verify(r => r.TryRevokeAsync(expectedHash, "uid", It.IsAny<CancellationToken>()), Times.Once);
        repoMock.VerifyAll();
        Mock.Get(identity).VerifyAll();
    }

    [Theory]
    [AutoMoqData]
    public async Task RevokeAll_CallsRepository(
        [Frozen] IRefreshTokenRepository refreshTokenRepository,
        [Frozen] IApplicationUserIdentityService identity,
        JwtTokenService jwtTokenService
    )
    {
        var ct = TestContext.Current.CancellationToken;
        var repoMock = Mock.Get(refreshTokenRepository);
        repoMock.Setup(r => r.RevokeAllForUserAsync("uid", It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        await jwtTokenService.RevokeAllRefreshTokensAsync("uid", ct);

        repoMock.Verify(r => r.RevokeAllForUserAsync("uid", It.IsAny<CancellationToken>()), Times.Once);
        repoMock.VerifyAll();
        Mock.Get(identity).VerifyAll();
    }

    [Theory]
    [AutoMoqData]
    public async Task Refresh_InvalidString_ReturnsNull(
        [Frozen] IRefreshTokenRepository refreshTokenRepository,
        [Frozen] IApplicationUserIdentityService identity,
        JwtTokenService jwtTokenService
    )
    {
        var ct = TestContext.Current.CancellationToken;
        var repoMock = Mock.Get(refreshTokenRepository);

        var result = await jwtTokenService.RefreshAsync("not-a-jwt", ct);
        Assert.Null(result);
        repoMock.VerifyAll();
        Mock.Get(identity).VerifyAll();
    }
}
