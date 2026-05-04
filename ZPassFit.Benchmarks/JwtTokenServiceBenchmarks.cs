using System.Security.Cryptography;
using System.Text;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Options;
using Moq;
using ZPassFit.Auth;
using ZPassFit.Data.Models;
using ZPassFit.Data.Repositories.Auth;
using ZPassFit.Services.Implementations;
using ZPassFit.Services.Interfaces;

namespace ZPassFit.Benchmarks;

[MemoryDiagnoser]
public class JwtTokenServiceBenchmarks
{
    private JwtTokenService _service = null!;
    private ApplicationUser _user = null!;
    private string _refreshToken = null!;
    private RefreshToken _storedRefresh = null!;

    [GlobalSetup]
    public void Setup()
    {
        var jwt = new JwtOptions
        {
            Secret = new string('k', 64),
            Issuer = "benchmark-issuer",
            Audience = "benchmark-audience",
            AccessTokenExpirationMinutes = 15,
            RefreshTokenExpirationDays = 30
        };

        var options = Options.Create(jwt);
        var refreshRepo = new Mock<IRefreshTokenRepository>(MockBehavior.Strict);
        var identity = new Mock<IApplicationUserIdentityService>(MockBehavior.Strict);

        _user = new ApplicationUser { Id = "bench-user", Email = "bench@example.com", UserName = "bench@example.com" };

        identity
            .Setup(s => s.GetRolesAsync(_user, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { "Admin", "Coach" });

        refreshRepo
            .Setup(r => r.AddAndSaveAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _service = new JwtTokenService(refreshRepo.Object, identity.Object, options);

        var pair = _service.CreateTokensAsync(_user, CancellationToken.None).GetAwaiter().GetResult();
        _refreshToken = pair.RefreshToken;

        var hash = HashToken(_refreshToken);
        _storedRefresh = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = _user.Id,
            TokenHash = hash,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow,
            RevokedAt = null
        };

        refreshRepo
            .Setup(r => r.FindActiveByHashAsync(hash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_storedRefresh);

        identity.Setup(s => s.FindByIdAsync(_user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_user);

        refreshRepo.Setup(r => r.RevokeAsync(_storedRefresh, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
    }

    [Benchmark]
    public async Task CreateTokensAsync_WithRoles()
    {
        await _service.CreateTokensAsync(_user, CancellationToken.None);
    }

    [Benchmark]
    public async Task RefreshAsync_Valid()
    {
        await _service.RefreshAsync(_refreshToken, CancellationToken.None);
    }

    private static string HashToken(string token)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hash);
    }
}
