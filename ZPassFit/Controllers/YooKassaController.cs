using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ZPassFit.Dto;
using ZPassFit.Middleware;
using ZPassFit.Options.Auth;
using ZPassFit.Options.Payments;
using ZPassFit.Services.Interfaces;

namespace ZPassFit.Controllers;

[ApiController]
[Tags("ЮKassa")]
[Route("[controller]")]
public class YooKassaController(
    IYooKassaService yooKassaService,
    IOptions<YooKassaOptions> yooKassaOptions
) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("webhook")]
    [EndpointSummary("Webhook ЮKassa")]
    [EndpointDescription("Обрабатывает уведомления от ЮKassa.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IResult> Post(CancellationToken cancellationToken)
    {
        if (!TryValidateBasicAuth(Request.Headers.Authorization.ToString(), yooKassaOptions.Value))
            return Results.Unauthorized();

        using var doc = await JsonDocument.ParseAsync(Request.Body, default, cancellationToken);
        await yooKassaService.HandleNotificationAsync(doc.RootElement, cancellationToken);
        return Results.Ok();
    }

    [HttpPost("checkout")]
    [Authorize(Roles = Roles.Client)]
    [EndpointSummary("Оплатить абонемент через ЮKassa")]
    [EndpointDescription(
        "Создаёт платеж в ЮKassa и возвращает URL для перехода пользователя на страницу оплаты.")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(StartYooKassaCheckoutResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IResult> StartYooKassaCheckout([FromBody] StartYooKassaCheckoutRequest request)
    {
        var user = HttpContext.GetRequiredCurrentApplicationUser();
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

        try
        {
            var result = await yooKassaService.StartCheckoutAsync(user.Id, request, ip);
            return Results.Ok(result);
        }
        catch (InvalidOperationException e)
        {
            return Results.BadRequest(new { error = e.Message });
        }
    }


    [HttpPost("sync/{paymentId:guid}")]
    [Authorize(Roles = Roles.Client)]
    [EndpointSummary("Синхронизировать статус платежа ЮKassa")]
    [EndpointDescription(
        "Запрашивает актуальный статус платежа в API ЮKassa и при успехе активирует абонемент.")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SyncYooKassaPaymentResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IResult> SyncYooKassaPayment(Guid paymentId)
    {
        var user = HttpContext.GetRequiredCurrentApplicationUser();
        var r = await yooKassaService.SyncPaymentFromApiAsync(user.Id, paymentId);

        return r.Code switch
        {
            "forbidden" => Results.Json(r, statusCode: StatusCodes.Status403Forbidden),
            "not_found" => Results.Json(r, statusCode: StatusCodes.Status404NotFound),
            "not_applicable" => Results.Json(r, statusCode: StatusCodes.Status400BadRequest),
            "api_error" => Results.Json(r, statusCode: StatusCodes.Status502BadGateway),
            _ => Results.Ok(r)
        };
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