using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZPassFit.Auth;
using ZPassFit.Data.Models.Memberships;
using ZPassFit.Data.Repositories.Memberships;
using ZPassFit.Dto;
using ZPassFit.Services.Interfaces;

namespace ZPassFit.Controllers;

[Authorize(Roles = Roles.AdminOrEmployee)]
[ApiController]
[Tags("Дашборд")]
[Route("dashboard/memberships")]
public class DashboardMembershipsController(
    IMembershipRepository membershipRepository,
    IMembershipService membershipService
) : ControllerBase
{
    private const int MinPage = 1;
    private const int MinPageSize = 1;
    private const int MaxPageSize = 100;

    [HttpGet]
    [EndpointSummary("Список абонементов")]
    [EndpointDescription(
        "Постраничный список абонементов с данными клиента и тарифа. Фильтр по статусу и поиск по ФИО, телефону, email и названию тарифа.")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedMembershipsResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> List(
        [FromQuery] MembershipStatus? status,
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

        var (items, total) = await membershipRepository.GetPagedAsync(
            status,
            q,
            (page - 1) * pageSize,
            pageSize,
            cancellationToken
        );

        var mapped = items.Select(MapListItem).ToList();
        return Results.Ok(new PagedMembershipsResponse(page, pageSize, total, mapped));
    }

    [HttpGet("{id:int}")]
    [EndpointSummary("Абонемент по ID")]
    [EndpointDescription("Карточка абонемента в дашборде с клиентом и тарифом.")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MembershipListItemResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetById([FromRoute] int id)
    {
        var membership = await membershipRepository.GetByIdAsync(id);
        return membership == null ? Results.NotFound() : Results.Ok(MapListItem(membership));
    }

    [HttpPost]
    [EndpointSummary("Выдать или продлить абонемент")]
    [EndpointDescription(
        "Создаёт абонемент или продлевает существующий у клиента по выбранному тарифу и длительности (как покупка, но без записи платежа).")]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(MembershipListItemResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> Create([FromBody] AdminSetMembershipRequest request)
    {
        try
        {
            var membership = await membershipService.AdminSetMembershipAsync(request);
            return Results.Created($"/dashboard/memberships/{membership.Id}", membership);
        }
        catch (InvalidOperationException e)
        {
            return Results.BadRequest(new { error = e.Message });
        }
    }

    [HttpPut("{id:int}")]
    [EndpointSummary("Обновить абонемент")]
    [EndpointDescription("Изменение статуса, тарифа и/или дат действия.")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MembershipListItemResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> Update([FromRoute] int id, [FromBody] UpdateMembershipRequest request)
    {
        try
        {
            var membership = await membershipService.AdminUpdateMembershipAsync(id, request);
            return membership == null ? Results.NotFound() : Results.Ok(membership);
        }
        catch (InvalidOperationException e)
        {
            return Results.BadRequest(new { error = e.Message });
        }
    }

    private static MembershipListItemResponse MapListItem(Membership m)
    {
        return new MembershipListItemResponse(
            m.Id,
            m.PlanId,
            m.Plan.Name,
            m.ClientId,
            m.Client.LastName,
            m.Client.FirstName,
            m.Status,
            m.ActivatedDate,
            m.ExpireDate
        );
    }
}
