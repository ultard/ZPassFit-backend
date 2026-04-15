using ZPassFit.Dto;

namespace ZPassFit.Services.Interfaces;

public interface IDashboardService
{
    Task<EmployeeDashboardResponse> GetEmployeeDashboardAsync(CancellationToken cancellationToken = default);

    Task<AdminDashboardResponse> GetAdminDashboardAsync(CancellationToken cancellationToken = default);
}
