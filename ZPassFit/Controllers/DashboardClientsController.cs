using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZPassFit.Auth;
using ZPassFit.Dto;
using ZPassFit.Services.Interfaces;

namespace ZPassFit.Controllers;

[Authorize(Roles = Roles.AdminOrEmployee)]
[ApiController]
[Tags("Дашборд")]
[Route("dashboard/clients")]
public class DashboardClientsController(IClientService clientService) : ControllerBase
{
    private const int MinPage = 1;
    private const int MinPageSize = 1;
    private const int MaxPageSize = 100;

    [HttpGet]
    [EndpointSummary("Список клиентов")]
    [EndpointDescription(
        "Постраничный список клиентов с поиском по ФИО, телефону и email.")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedClientsResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> List(
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

        var data = await clientService.SearchPagedAsync(q, page, pageSize, cancellationToken);
        return Results.Ok(data);
    }

    [HttpGet("{id:guid}")]
    [EndpointSummary("Клиент по ID")]
    [EndpointDescription("Полный профиль клиента для карточки в дашборде.")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ClientResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetById([FromRoute] Guid id)
    {
        var client = await clientService.GetByIdAsync(id);
        return client == null ? Results.NotFound() : Results.Ok(client);
    }
}
