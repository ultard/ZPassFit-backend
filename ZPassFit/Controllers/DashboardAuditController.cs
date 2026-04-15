using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZPassFit.Auth;
using ZPassFit.Data.Models.Audit;
using ZPassFit.Data.Repositories.Audit;
using ZPassFit.Dto;

namespace ZPassFit.Controllers;

[Authorize(Roles = Roles.Admin)]
[ApiController]
[Tags("Дашборд")]
[Route("dashboard/audit")]
public class DashboardAuditController(IAuditLogRepository auditLogRepository) : ControllerBase
{
    private const int MinPage = 1;
    private const int MinPageSize = 1;
    private const int MaxPageSize = 100;

    [HttpGet]
    [EndpointSummary("Журнал аудита")]
    [EndpointDescription(
        "Список записей аудита. Фильтры: интервал по UTC, подстрока в Action и EntityType.")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedAuditLogsResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> List(
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] string? action,
        [FromQuery] string? entityType,
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

        var (items, total) = await auditLogRepository.GetPagedAsync(
            fromUtc,
            toUtc,
            action,
            entityType,
            (page - 1) * pageSize,
            pageSize,
            cancellationToken
        );

        var mapped = items.Select(Map).ToList();
        return Results.Ok(new PagedAuditLogsResponse(page, pageSize, total, mapped));
    }

    [HttpGet("{id:long}")]
    [EndpointSummary("Запись аудита по ID")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AuditLogResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetById([FromRoute] long id, CancellationToken cancellationToken = default)
    {
        var row = await auditLogRepository.GetByIdAsync(id, cancellationToken);
        return row == null ? Results.NotFound() : Results.Ok(Map(row));
    }

    private static AuditLogResponse Map(AuditLog log)
    {
        return new AuditLogResponse(
            log.Id,
            log.OccurredAtUtc,
            log.UserId,
            log.User?.Email,
            log.Action,
            log.EntityType,
            log.EntityId,
            log.Details,
            log.IpAddress
        );
    }
}
