using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZPassFit.Auth;
using ZPassFit.Data.Models.Clients;
using ZPassFit.Data.Repositories.Clients;
using ZPassFit.Dto;

namespace ZPassFit.Controllers;

[Authorize(Roles = Roles.AdminOrEmployee)]
[ApiController]
[Tags("Дашборд — бонусы")]
[Route("dashboard/bonuses")]
public class DashboardBonusesController(IBonusTransactionRepository bonusTransactionRepository) : ControllerBase
{
    private const int MinPage = 1;
    private const int MinPageSize = 1;
    private const int MaxPageSize = 100;

    [HttpGet]
    [EndpointSummary("Список транзакций бонусов")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedBonusTransactionsResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> List(
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] Guid? clientId,
        [FromQuery] BonusTransactionType? type,
        [FromQuery] string? search,
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

        var (items, total) = await bonusTransactionRepository.GetPagedAsync(
            fromUtc,
            toUtc,
            clientId,
            type,
            search,
            (page - 1) * pageSize,
            pageSize,
            cancellationToken
        );

        var mapped = items.Select(MapListItem).ToList();
        return Results.Ok(new PagedBonusTransactionsResponse(page, pageSize, total, mapped));
    }

    [HttpGet("{id:guid}")]
    [EndpointSummary("Транзакция бонусов по ID")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(BonusTransactionListItemResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetById([FromRoute] Guid id, CancellationToken cancellationToken = default)
    {
        var row = await bonusTransactionRepository.GetByIdAsync(id);
        return row == null ? Results.NotFound() : Results.Ok(MapListItem(row));
    }

    private static BonusTransactionListItemResponse MapListItem(BonusTransaction t)
    {
        return new BonusTransactionListItemResponse(
            t.Id,
            t.Type,
            t.CreateDate,
            t.ExpireDate,
            t.ClientId,
            t.Client.LastName,
            t.Client.FirstName
        );
    }
}
