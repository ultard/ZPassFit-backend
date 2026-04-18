using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZPassFit.Auth;
using ZPassFit.Dto;
using ZPassFit.Services.Interfaces;

namespace ZPassFit.Controllers;

[Authorize(Roles = Roles.AdminOrEmployee)]
[ApiController]
[Tags("Дашборд")]
[Route("[controller]")]
public class DashboardController(IDashboardService dashboardService) : ControllerBase
{
    [HttpGet("overview")]
    [EndpointSummary("Сводка для главной дашборда")]
    [EndpointDescription(
        "KPI за выбранный календарный месяц (часовой пояс клуба из конфигурации), сравнение с предыдущим месяцем и ряды для графиков.")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DashboardOverviewResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IResult> Overview(
        [FromQuery] int? year,
        [FromQuery] int? month,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var overview = await dashboardService.GetOverviewAsync(year, month, cancellationToken);
            return Results.Ok(overview);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }
}
