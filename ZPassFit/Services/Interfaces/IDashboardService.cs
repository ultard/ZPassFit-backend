using ZPassFit.Dto;

namespace ZPassFit.Services.Interfaces;

public interface IDashboardService
{
    Task<DashboardOverviewResponse> GetOverviewAsync(
        int? year,
        int? month,
        CancellationToken cancellationToken = default
    );
}
