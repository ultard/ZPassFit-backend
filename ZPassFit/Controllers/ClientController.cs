using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZPassFit.Auth;
using ZPassFit.Dto;
using ZPassFit.Middleware;
using ZPassFit.Services.Interfaces;

namespace ZPassFit.Controllers;

[Authorize]
[ApiController]
[Tags("Клиенты")]
[Route("[controller]")]
public class ClientController(
    IClientService clientService,
    IMembershipService membershipService
) : ControllerBase
{
    [HttpGet("profile")]
    [Authorize(Roles = Roles.Client)]
    [EndpointSummary("Профиль клиента")]
    [EndpointDescription("Возвращает профиль клиента текущего пользователя.")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ClientResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetProfile()
    {
        var user = HttpContext.GetRequiredCurrentApplicationUser();

        var client = await clientService.GetMeAsync(user.Id);
        return client == null ? Results.NotFound() : Results.Ok(client);
    }

    [HttpPut("profile")]
    [Authorize(Roles = Roles.Client)]
    [EndpointSummary("Обновить профиль")]
    [EndpointDescription("Изменяет ФИО, дату рождения и пол текущего клиента.")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ClientResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateProfile([FromBody] UpdateClientProfileRequest request)
    {
        var user = HttpContext.GetRequiredCurrentApplicationUser();

        try
        {
            var client = await clientService.UpdateMyProfileAsync(user.Id, request);
            return client == null ? Results.NotFound() : Results.Ok(client);
        }
        catch (InvalidOperationException e)
        {
            return Results.BadRequest(new { error = e.Message });
        }
    }

    [HttpGet("level")]
    [Authorize(Roles = Roles.Client)]
    [EndpointSummary("Мой уровень лояльности")]
    [EndpointDescription(
        "Возвращает активный уровень текущего пользователя, следующий уровень в цепочке и сколько посещений осталось до следующего уровня.")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MyClientLevelResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetLevel()
    {
        var user = HttpContext.GetRequiredCurrentApplicationUser();

        var level = await clientService.GetMyActiveLevelAsync(user.Id);
        return level == null ? Results.NotFound() : Results.Ok(level);
    }

    [HttpGet("membership")]
    [Authorize(Roles = Roles.Client)]
    [EndpointSummary("Мой абонемент")]
    [EndpointDescription("Возвращает текущий абонемент текущего пользователя.")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MembershipResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetMembership()
    {
        var user = HttpContext.GetRequiredCurrentApplicationUser();

        var membership = await membershipService.GetMyMembershipAsync(user.Id);
        return membership == null ? Results.NotFound() : Results.Ok(membership);
    }

    [HttpGet("payments")]
    [Authorize(Roles = Roles.Client)]
    [EndpointSummary("Мои платежи")]
    [EndpointDescription("Возвращает историю платежей текущего пользователя.")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<PaymentResponse>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetPayments()
    {
        var user = HttpContext.GetRequiredCurrentApplicationUser();

        var payments = await membershipService.GetMyPaymentsAsync(user.Id);
        return Results.Ok(payments);
    }
}