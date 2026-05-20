using System.Net.Http.Headers;
using System.Text;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;

namespace ZPassFit.YooKassa;

/// <summary>
///     HTTP Basic authentication as required by the YooKassa REST API (<c>shopId:secretKey</c>, Base64).
/// </summary>
public sealed class YooKassaBasicAuthenticationProvider : IAuthenticationProvider
{
    private readonly AuthenticationHeaderValue _authorization;

    public YooKassaBasicAuthenticationProvider(string shopId, string secretKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shopId);
        ArgumentException.ThrowIfNullOrWhiteSpace(secretKey);
        var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{shopId}:{secretKey}"));
        _authorization = new AuthenticationHeaderValue("Basic", token);
    }

    /// <inheritdoc />
    public Task AuthenticateRequestAsync(
        RequestInformation request,
        Dictionary<string, object>? additionalAuthenticationContext = null,
        CancellationToken cancellationToken = default)
    {
        request.Headers.TryAdd("Authorization", $"{_authorization.Scheme} {_authorization.Parameter}");
        return Task.CompletedTask;
    }
}