using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZPassFit.Auth;
using ZPassFit.Dto;
using ZPassFit.Services.Interfaces;

namespace ZPassFit.Controllers;

[Authorize(Roles = Roles.AdminOrEmployee)]
[ApiController]
[Tags("Дашборд")]
[Route("dashboard/levels")]
public class DashboardLevelsController(ILevelService levelService) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("Список уровней")]
    [EndpointDescription("Все доступные уровни лояльности и их цепочка предыдущих уровней.")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<LevelResponse>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IResult> List(CancellationToken cancellationToken)
    {
        var levels = await levelService.GetAllAsync(cancellationToken);
        return Results.Ok(levels);
    }

    [HttpGet("{id:guid}")]
    [EndpointSummary("Уровень по ID")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LevelResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var level = await levelService.GetByIdAsync(id, cancellationToken);
        return level == null ? Results.NotFound() : Results.Ok(level);
    }

    [HttpPost]
    [EndpointSummary("Создать уровень")]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(LevelResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> Create([FromBody] CreateLevelRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var level = await levelService.CreateAsync(request, cancellationToken);
            return Results.Created($"/dashboard/levels/{level.Id}", level);
        }
        catch (InvalidOperationException e)
        {
            return Results.BadRequest(new { error = e.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [EndpointSummary("Обновить уровень")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LevelResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateLevelRequest request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var level = await levelService.UpdateAsync(id, request, cancellationToken);
            return level == null ? Results.NotFound() : Results.Ok(level);
        }
        catch (InvalidOperationException e)
        {
            return Results.BadRequest(new { error = e.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    [EndpointSummary("Удалить уровень")]
    [EndpointDescription("Нельзя удалить уровень, назначенный клиентам или указанный как предыдущий у другого уровня.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> Delete([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await levelService.DeleteAsync(id, cancellationToken);
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
