using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ZPassFit.Auth;
using ZPassFit.Data.Models;
using ZPassFit.Dto;
using ZPassFit.Services.Interfaces;

namespace ZPassFit.Controllers;

[Authorize]
[ApiController]
[Tags("Клиенты")]
[Route("[controller]")]
public class ClientController(
    IClientService clientService,
    UserManager<ApplicationUser> userManager
    ) : ControllerBase
{
    [HttpGet("me")]
    [Authorize(Roles = Roles.Client)]
    [EndpointSummary("Профиль клиента")]
    [EndpointDescription("Возвращает профиль клиента текущего авторизованного пользователя.")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ClientResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetMe()
    {
        var user = await userManager.GetUserAsync(HttpContext.User);
        if (user == null) return Results.Unauthorized();

        var client = await clientService.GetMeAsync(user.Id);
        return client == null ? Results.NotFound() : Results.Ok(client);
    }

    [HttpPut("me")]
    [Authorize(Roles = Roles.Client)]
    [EndpointSummary("Создать/обновить профиль клиента")]
    [EndpointDescription("Создаёт профиль клиента для текущего пользователя, либо обновляет существующий.")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ClientResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> UpsertMe([FromBody] UpsertClientMeRequest request)
    {
        var user = await userManager.GetUserAsync(HttpContext.User);
        if (user == null) return Results.Unauthorized();

        var client = await clientService.UpsertMeAsync(user.Id, request);
        return Results.Ok(client);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = Roles.AdminOrEmployee)]
    [EndpointSummary("Клиент по ID")]
    [EndpointDescription("Возвращает информацию о клиенте по его идентификатору.")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ClientResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetById([FromRoute] Guid id)
    {
        var client = await clientService.GetByIdAsync(id);
        return client == null ? Results.NotFound() : Results.Ok(client);
    }
}