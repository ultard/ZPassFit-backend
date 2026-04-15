using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZPassFit.Auth;
using ZPassFit.Dto;
using ZPassFit.Middleware;
using ZPassFit.Services.Interfaces;

namespace ZPassFit.Controllers;

[Authorize]
[ApiController]
[Tags("Абонементы")]
[Route("[controller]")]
public class MembershipController(
    IMembershipService membershipService
) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("membership/plans")]
    [EndpointSummary("Список тарифов")]
    [EndpointDescription("Возвращает список доступных тарифов абонементов.")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<MembershipPlanResponse>))]
    public async Task<IResult> GetPlans()
    {
        var plans = await membershipService.GetPlansAsync();
        return Results.Ok(plans);
    }

    [HttpGet("membership/me")]
    [Authorize(Roles = Roles.Client)]
    [EndpointSummary("Мой абонемент")]
    [EndpointDescription("Возвращает текущий абонемент авторизованного клиента.")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MembershipResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetMyMembership()
    {
        var user = HttpContext.GetRequiredCurrentApplicationUser();

        var membership = await membershipService.GetMyMembershipAsync(user.Id);
        return membership == null ? Results.NotFound() : Results.Ok(membership);
    }

    [HttpPost("membership/buy")]
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

    [HttpGet("payments/me")]
    [Authorize(Roles = Roles.Client)]
    [EndpointSummary("Мои платежи")]
    [EndpointDescription("Возвращает историю платежей текущего клиента.")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<PaymentResponse>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetMyPayments()
    {
        var user = HttpContext.GetRequiredCurrentApplicationUser();

        var payments = await membershipService.GetMyPaymentsAsync(user.Id);
        return Results.Ok(payments);
    }
}