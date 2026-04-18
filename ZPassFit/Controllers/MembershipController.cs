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
    [HttpGet("plans")]
    [EndpointSummary("Список тарифов")]
    [EndpointDescription("Возвращает список доступных тарифов абонементов.")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<MembershipPlanResponse>))]
    public async Task<IResult> GetPlans()
    {
        var plans = await membershipService.GetPlansAsync();
        return Results.Ok(plans);
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
}