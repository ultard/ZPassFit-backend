using ZPassFit.Dto;

namespace ZPassFit.Services.Interfaces;

public interface IDashboardService
{
    Task<DashboardOverviewResponse> GetOverviewAsync(
        int? year,
        int? month,
        CancellationToken cancellationToken = default
    );

    Task<ClientStatsResponse?> GetClientStatsAsync(
        Guid clientId,
        int? year,
        int? month,
        CancellationToken cancellationToken = default
    );
}