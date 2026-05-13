using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Text.Json;
using ZPassFit.Payments;
using ZPassFit.Services.Interfaces;

namespace ZPassFit.Controllers;

[ApiController]
[AllowAnonymous]
[Route("webhooks/yookassa")]
public class YooKassaWebhookController(
    IYooKassaService yooKassaService,
    IOptions<YooKassaOptions> yooKassaOptions
) : ControllerBase
{
    [HttpPost]
    [EndpointSummary("Webhook ЮKassa")]
    public async Task<IResult> Post(CancellationToken cancellationToken)
    {
        if (!TryValidateBasicAuth(Request.Headers.Authorization.ToString(), yooKassaOptions.Value))
            return Results.Unauthorized();

        using var doc = await JsonDocument.ParseAsync(Request.Body, default, cancellationToken);
        await yooKassaService.HandleNotificationAsync(doc.RootElement, cancellationToken);
        return Results.Ok();
    }

    private static bool TryValidateBasicAuth(string? authorizationHeader, YooKassaOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ShopId) || string.IsNullOrWhiteSpace(options.SecretKey))
            return false;

        if (!AuthenticationHeaderValue.TryParse(authorizationHeader, out var auth)
            || !auth.Scheme.Equals("Basic", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrEmpty(auth.Parameter))
            return false;

        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(auth.Parameter));
            var colon = decoded.IndexOf(':');
            if (colon <= 0)
                return false;

            var login = decoded[..colon];
            var password = decoded[(colon + 1)..];
            return login == options.ShopId && password == options.SecretKey;
        }
        catch
        {
            return false;
        }
    }
}
