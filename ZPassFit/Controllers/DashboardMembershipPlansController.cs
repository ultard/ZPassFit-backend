using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZPassFit.Auth;
using ZPassFit.Dto;
using ZPassFit.Services.Interfaces;

namespace ZPassFit.Controllers;

[Authorize(Roles = Roles.AdminOrEmployee)]
[ApiController]
[Tags("Дашборд — тарифы")]
[Route("dashboard/membership-plans")]
public class DashboardMembershipPlansController(IMembershipService membershipService) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("Список тарифных планов")]
    [EndpointDescription("Все тарифы абонементов для настройки и отображения в дашборде.")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<MembershipPlanResponse>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IResult> List()
    {
        var plans = await membershipService.GetPlansAsync();
        return Results.Ok(plans);
    }

    [HttpGet("{id:int}")]
    [EndpointSummary("Тарифный план по ID")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MembershipPlanResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetById([FromRoute] int id)
    {
        var plan = await membershipService.GetPlanByIdAsync(id);
        return plan == null ? Results.NotFound() : Results.Ok(plan);
    }

    [HttpPost]
    [EndpointSummary("Создать тарифный план")]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(MembershipPlanResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> Create([FromBody] CreateMembershipPlanRequest request)
    {
        try
        {
            var plan = await membershipService.CreatePlanAsync(request);
            return Results.Created($"/dashboard/membership-plans/{plan.Id}", plan);
        }
        catch (InvalidOperationException e)
        {
            return Results.BadRequest(new { error = e.Message });
        }
    }

    [HttpPut("{id:int}")]
    [EndpointSummary("Обновить тарифный план")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MembershipPlanResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> Update([FromRoute] int id, [FromBody] UpdateMembershipPlanRequest request)
    {
        try
        {
            var plan = await membershipService.UpdatePlanAsync(id, request);
            return plan == null ? Results.NotFound() : Results.Ok(plan);
        }
        catch (InvalidOperationException e)
        {
            return Results.BadRequest(new { error = e.Message });
        }
    }

    [HttpDelete("{id:int}")]
    [EndpointSummary("Удалить тарифный план")]
    [EndpointDescription("Нельзя удалить план, если на него ссылаются абонементы.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> Delete([FromRoute] int id)
    {
        try
        {
            await membershipService.DeletePlanAsync(id);
            return Results.NoContent();
        }
        catch (InvalidOperationException e)
        {
            if (e.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
                return Results.NotFound();

            return Results.Conflict(new { error = e.Message });
        }
    }
}
