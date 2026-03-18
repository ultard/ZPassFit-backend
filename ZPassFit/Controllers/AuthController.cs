using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ZPassFit.Data.Models;
using ZPassFit.Dto;

namespace ZPassFit.Controllers;

[ApiController]
[Tags("Авторизация")]
[Route("[controller]")]
public class AuthController(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager
    ) : ControllerBase
{
    [HttpPost("/register")]
    [EndpointSummary("Регистрация")]
    [EndpointDescription("Создаёт нового пользователя.")]
    [ProducesResponseType(StatusCodes.Status201Created, Description = "Регистрация прошла успешно")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Description = "Не удалось зарегистрировать пользователя")]
    public async Task<IResult> Register([FromBody] RegisterRequest request)
    {
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email
        };

        var result = await userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            return Results.BadRequest(result.Errors);
        }

        await signInManager.SignInAsync(user, isPersistent: true);
        return Results.Created();
    }

    [HttpPost("/login")]
    [EndpointSummary("Вход")]
    [EndpointDescription("Входит в существующего пользователя.")]
    [ProducesResponseType(StatusCodes.Status202Accepted, Description = "Вход произошёл успешно")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Description = "Неверная почта или пароль")]
    public async Task<IResult> Login([FromBody] LoginRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user == null)
            return Results.Unauthorized();

        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

        if (!result.Succeeded)
            return Results.Unauthorized();

        await signInManager.SignInAsync(user, isPersistent: true);
        return Results.Accepted();
    }

    [Authorize]
    [HttpPost("/logout")]
    [EndpointSummary("Выход")]
    [EndpointDescription("Удаляет куки и сессию текущего пользователя.")]
    [ProducesResponseType(StatusCodes.Status204NoContent, Description = "Сессия успешно закрыта")]
    public async Task<IResult> Logout()
    {
        await signInManager.SignOutAsync();
        return Results.NoContent();
    }
}