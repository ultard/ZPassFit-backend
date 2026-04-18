using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ZPassFit.Auth;
using ZPassFit.Data.Models;
using ZPassFit.Data.Models.Clients;
using ZPassFit.Data.Repositories.Clients;
using ZPassFit.Dto;
using ZPassFit.Services.Interfaces;

namespace ZPassFit.Controllers;

[ApiController]
[Tags("Авторизация")]
[Route("[controller]")]
public class AuthController(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    IJwtTokenService jwtTokenService,
    IClientRepository clientRepository
) : ControllerBase
{
    [HttpPost("register")]
    [EndpointSummary("Регистрация")]
    [EndpointDescription("Создаёт нового пользователя (клиента). Требуется подтверждение администратором.")]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status201Created, Description = "Регистрация прошла успешно")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Description = "Не удалось зарегистрироватся")]
    public async Task<IResult> Register([FromBody] RegisterRequest request)
    {
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email
        };

        var result = await userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded) return Results.BadRequest(result.Errors);

        var addRole = await userManager.AddToRoleAsync(user, Roles.Client);
        if (!addRole.Succeeded)
            return Results.BadRequest(addRole.Errors);

        var client = new Client
        {
            UserId = user.Id,
            LastName = request.LastName.Trim(),
            FirstName = request.FirstName.Trim(),
            MiddleName = request.MiddleName.Trim(),
            BirthDate = request.BirthDate,
            Gender = request.Gender,
            Phone = request.Phone.Trim(),
            Email = request.Email.Trim()
        };

        await clientRepository.AddAsync(client);

        return Results.Created("/auth/login", new RegisterResponse("Pending"));
    }

    [HttpPost("login")]
    [EndpointSummary("Вход")]
    [EndpointDescription("Проверяет учётные данные.")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK, Description = "Вход выполнен успешно")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Description = "Неверная почта или пароль")]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Description = "Аккаунт не подтверждён или заблокирован")]
    public async Task<IResult> Login([FromBody] LoginRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user == null)
            return Results.Unauthorized();

        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, true);

        if (!result.Succeeded)
            return Results.Unauthorized();

        if (await userManager.IsInRoleAsync(user, Roles.Client))
        {
            var client = await clientRepository.GetByUserIdAsync(user.Id);
            if (client is not { Status: ClientStatus.Active })
                return Results.Forbid();
        }

        var tokens = await jwtTokenService.CreateTokensAsync(user);
        var body = new AuthResponse(tokens.AccessToken, tokens.RefreshToken, tokens.AccessTokenExpiresAtUtc);
        return Results.Ok(body);
    }

    [HttpPost("refresh")]
    [EndpointSummary("Обновление токенов")]
    [EndpointDescription("По действительному токену выдаёт новую пару.")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> Refresh([FromBody] RefreshRequest request)
    {
        var tokens = await jwtTokenService.RefreshAsync(request.RefreshToken);
        if (tokens == null)
            return Results.Unauthorized();

        var body = new AuthResponse(tokens.AccessToken, tokens.RefreshToken, tokens.AccessTokenExpiresAtUtc);
        return Results.Ok(body);
    }

    [Authorize]
    [HttpPost("logout")]
    [EndpointSummary("Выход")]
    [EndpointDescription("Отзывает refresh-токены.")]
    [ProducesResponseType(StatusCodes.Status204NoContent, Description = "Выход выполнен")]
    public async Task<IResult> Logout([FromBody] LogoutRequest? request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Results.Unauthorized();

        if (request?.RefreshToken is { } token)
            await jwtTokenService.RevokeRefreshTokenAsync(token, userId);
        else
            await jwtTokenService.RevokeAllRefreshTokensAsync(userId);

        return Results.NoContent();
    }
}
