using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ZPassFit.Data.Models;
using ZPassFit.Dto;
using ZPassFit.Services.Interfaces;

namespace ZPassFit.Controllers;

[ApiController]
[Tags("Авторизация")]
[Route("[controller]")]
public class AuthController(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    IJwtTokenService jwtTokenService
) : ControllerBase
{
    [HttpPost("register")]
    [EndpointSummary("Регистрация")]
    [EndpointDescription("Создаёт нового пользователя.")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created, Description = "Регистрация прошла успешно")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Description = "Не удалось зарегистрировать пользователя")]
    public async Task<IResult> Register([FromBody] RegisterRequest request)
    {
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email
        };

        var result = await userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded) return Results.BadRequest(result.Errors);

        var tokens = await jwtTokenService.CreateTokensAsync(user);
        var body = new AuthResponse(tokens.AccessToken, tokens.RefreshToken, tokens.AccessTokenExpiresAtUtc);
        return Results.Created("/auth/login", body);
    }

    [HttpPost("login")]
    [EndpointSummary("Вход")]
    [EndpointDescription("Проверяет учётные данные.")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK, Description = "Вход выполнен успешно")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Description = "Неверная почта или пароль")]
    public async Task<IResult> Login([FromBody] LoginRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user == null)
            return Results.Unauthorized();

        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, true);

        if (!result.Succeeded)
            return Results.Unauthorized();

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
