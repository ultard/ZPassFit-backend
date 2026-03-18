using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ZPassFit.Auth;
using ZPassFit.Data.Models;
using ZPassFit.Dto;
using ZPassFit.Services;
using ZPassFit.Services.Interfaces;

namespace ZPassFit.Controllers;

[Authorize]
[ApiController]
[Tags("Посещения")]
[Route("[controller]")]
public class AttendanceController(
    UserManager<ApplicationUser> userManager,
    IAttendanceService attendanceService
    ) : ControllerBase
{
    [HttpGet("visits")]
    [Authorize(Roles = Roles.Client)]
    [EndpointSummary("Текущее посещение")]
    [EndpointDescription("Возвращает открытое (не завершённое) посещение текущего клиента, если оно есть.")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(VisitLogResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetVisits()
    {
        var user = await userManager.GetUserAsync(HttpContext.User);

        if (user == null)
            return Results.Unauthorized();

        var visit = await attendanceService.GetOpenVisitAsync(user.Id);

        return Results.Ok(visit);
    }

    [HttpGet("visits/history")]
    [Authorize(Roles = Roles.Client)]
    [EndpointSummary("История посещений")]
    [EndpointDescription("Возвращает историю посещений текущего клиента (от новых к старым).")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<VisitLogResponse>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetVisitHistory()
    {
        var user = await userManager.GetUserAsync(HttpContext.User);
        if (user == null) return Results.Unauthorized();

        var visits = await attendanceService.GetVisitHistoryAsync(user.Id);
        return Results.Ok(visits);
    }

    [HttpPost("qr_session")]
    [Authorize(Roles = Roles.Client)]
    [EndpointSummary("Создать QR-сессию")]
    [EndpointDescription("Создаёт краткоживущую QR-сессию для входа клиента в клуб. Возвращает токен и время истечения.")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(QrSessionResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateSession()
    {
        var user = await userManager.GetUserAsync(HttpContext.User);
        if (user == null) return Results.Unauthorized();

        try
        {
            var session = await attendanceService.CreateQrSessionAsync(user.Id);
            return Results.Ok(session);
        }
        catch (InvalidOperationException e)
        {
            return Results.BadRequest(new { error = e.Message });
        }
    }

    [HttpPost("checkin/{token}")]
    [Authorize(Roles = Roles.AdminOrEmployee)]
    [EndpointSummary("Вход по QR")]
    [EndpointDescription("Открывает посещение по QR-токену (обычно сканируется на ресепшене). При успехе удаляет QR-сессию.")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(VisitLogResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CheckIn([FromRoute] string token)
    {
        try
        {
            var visit = await attendanceService.CheckInByTokenAsync(token);
            return Results.Ok(visit);
        }
        catch (InvalidOperationException e)
        {
            return Results.BadRequest(new { error = e.Message });
        }
    }

    [HttpPost("checkout")]
    [Authorize(Roles = Roles.Client)]
    [EndpointSummary("Выход")]
    [EndpointDescription("Закрывает текущее посещение клиента, проставляя время выхода.")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(VisitLogResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CheckOut()
    {
        var user = await userManager.GetUserAsync(HttpContext.User);
        if (user == null) return Results.Unauthorized();

        try
        {
            var visit = await attendanceService.CheckOutAsync(user.Id);
            return Results.Ok(visit);
        }
        catch (InvalidOperationException e)
        {
            return Results.BadRequest(new { error = e.Message });
        }
    }
}