using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZPassFit.Auth;
using ZPassFit.Data.Models.Attendance;
using ZPassFit.Data.Repositories.Attendance;
using ZPassFit.Dto;

namespace ZPassFit.Controllers;

[Authorize(Roles = Roles.AdminOrEmployee)]
[ApiController]
[Tags("Дашборд — посещения")]
[Route("dashboard/visits")]
public class DashboardVisitsController(IVisitLogRepository visitLogRepository) : ControllerBase
{
    private const int MinPage = 1;
    private const int MinPageSize = 1;
    private const int MaxPageSize = 100;

    [HttpGet]
    [EndpointSummary("Список посещений")]
    [EndpointDescription(
        "Постраничный журнал посещений. Фильтры: интервал входа по UTC, клиент, только открытые/закрытые, поиск по ФИО, телефону и email клиента.")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedVisitLogsResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> List(
        [FromQuery] DateTime? enterFromUtc,
        [FromQuery] DateTime? enterToUtc,
        [FromQuery] Guid? clientId,
        [FromQuery] bool? openOnly,
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default
    )
    {
        if (page < MinPage || pageSize < MinPageSize || pageSize > MaxPageSize)
        {
            return Results.BadRequest(
                new
                {
                    error = $"page must be >= {MinPage}, pageSize must be between {MinPageSize} and {MaxPageSize}."
                }
            );
        }

        var (items, total) = await visitLogRepository.GetPagedAsync(
            enterFromUtc,
            enterToUtc,
            clientId,
            openOnly,
            q,
            (page - 1) * pageSize,
            pageSize,
            cancellationToken
        );

        var mapped = items.Select(MapListItem).ToList();
        return Results.Ok(new PagedVisitLogsResponse(page, pageSize, total, mapped));
    }

    [HttpGet("{id:guid}")]
    [EndpointSummary("Посещение по ID")]
    [EndpointDescription("Запись журнала посещений для карточки в дашборде.")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(VisitLogResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetById([FromRoute] Guid id, CancellationToken cancellationToken = default)
    {
        var visit = await visitLogRepository.GetByIdAsync(id, cancellationToken);
        return visit == null
            ? Results.NotFound()
            : Results.Ok(
                new VisitLogResponse(visit.Id, visit.EnterDate, visit.LeaveDate, visit.MembershipId, visit.ClientId)
            );
    }

    private static VisitLogListItemResponse MapListItem(VisitLog v)
    {
        return new VisitLogListItemResponse(
            v.Id,
            v.EnterDate,
            v.LeaveDate,
            v.MembershipId,
            v.ClientId,
            v.Client.LastName,
            v.Client.FirstName
        );
    }
}
