using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZPassFit.Auth;
using ZPassFit.Dto;
using ZPassFit.Services.Interfaces;

namespace ZPassFit.Controllers;

[Authorize]
[ApiController]
[Tags("Дашборд")]
[Route("[controller]")]
public class DashboardController(IDashboardService dashboardService) : ControllerBase
{
    [HttpGet("employee")]
    [Authorize(Roles = Roles.AdminOrEmployee)]
    [EndpointSummary("Дашборд сотрудника")]
    [EndpointDescription(
        "Показатели для ресепшена: визиты за текущие сутки по UTC, открытые посещения, неистёкшие QR-сессии, последние check-in.")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(EmployeeDashboardResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IResult> GetEmployeeDashboard(CancellationToken cancellationToken)
    {
        var data = await dashboardService.GetEmployeeDashboardAsync(cancellationToken);
        return Results.Ok(data);
    }

    [HttpGet("admin")]
    [Authorize(Roles = Roles.Admin)]
    [EndpointSummary("Дашборд администратора")]
    [EndpointDescription(
        "Включает блок Staff (как у сотрудника) и дополнительно: число клиентов, активных абонементов, количество и сумму завершённых платежей за сутки по UTC.")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AdminDashboardResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IResult> GetAdminDashboard(CancellationToken cancellationToken)
    {
        var data = await dashboardService.GetAdminDashboardAsync(cancellationToken);
        return Results.Ok(data);
    }
}
