using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ZPassFit.Auth;
using ZPassFit.Data.Models.Memberships;
using ZPassFit.Dto;
using ZPassFit.Middleware;
using ZPassFit.Payments;
using ZPassFit.Services.Interfaces;

namespace ZPassFit.Controllers;

[Authorize]
[ApiController]
[Tags("Абонементы")]
[Route("[controller]")]
public class MembershipController(
    IMembershipService membershipService,
    IOptions<PaymentMethodsOptions> paymentMethodsOptions
) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("plans")]
    [EndpointSummary("Список тарифов")]
    [EndpointDescription("Возвращает список доступных тарифов абонементов.")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<MembershipPlanResponse>))]
    public async Task<IResult> GetPlans()
    {
        var plans = await membershipService.GetPlansAsync();
        return Results.Ok(plans);
    }

    [HttpGet("payment-methods")]
    [Authorize(Roles = Roles.AdminOrEmployee)]
    [EndpointSummary("Способы оплаты")]
    [EndpointDescription(
        "Доступные способы оплаты для клиентского сценария покупки абонемента.")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PaymentMethodsSettingsResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IResult PaymentMethods()
    {
        var o = paymentMethodsOptions.Value;
        var methods = new[]
        {
            new PaymentMethodSettingResponse(
                PaymentMethod.Cash,
                "cash",
                "Наличные",
                o.CashEnabled,
                "Оплата на ресепшене клуба."),
            new PaymentMethodSettingResponse(
                PaymentMethod.Card,
                "card",
                "Банковская карта",
                o.CardEnabled,
                "Оплата картой на ресепшене или через эквайринг (по настройке клуба)."),
            new PaymentMethodSettingResponse(
                PaymentMethod.Balance,
                "balance",
                "Баланс клиента",
                o.BalanceEnabled,
                "Списание с внутреннего бонусного/депозитного баланса.")
        };

        return Results.Ok(new PaymentMethodsSettingsResponse(methods));
    }

    [HttpPost("buy")]
    [Authorize(Roles = Roles.Client)]
    [EndpointSummary("Купить абонемент")]
    [EndpointDescription(
        "Покупает/активирует абонемент по выбранному тарифу и длительности. Создаёт запись об оплате.")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MembershipResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> BuyMembership([FromBody] BuyMembershipRequest request)
    {
        var user = HttpContext.GetRequiredCurrentApplicationUser();

        try
        {
            var membership = await membershipService.BuyMembershipAsync(user.Id, request);
            return Results.Ok(membership);
        }
        catch (InvalidOperationException e)
        {
            return Results.BadRequest(new { error = e.Message });
        }
    }

    [HttpPost("cancel")]
    [Authorize(Roles = Roles.Client)]
    [EndpointSummary("Отменить абонемент")]
    [EndpointDescription("Отключает автопродление: за текущий период списания продолжаются, а следующие периоды не списываются.")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MembershipResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CancelMembership()
    {
        var user = HttpContext.GetRequiredCurrentApplicationUser();

        try
        {
            var membership = await membershipService.CancelMembershipAsync(user.Id);
            return Results.Ok(membership);
        }
        catch (InvalidOperationException e)
        {
            return Results.BadRequest(new { error = e.Message });
        }
    }
}