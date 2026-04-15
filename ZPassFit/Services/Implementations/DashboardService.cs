using ZPassFit.Data.Models.Attendance;
using ZPassFit.Data.Repositories.Attendance;
using ZPassFit.Data.Repositories.Clients;
using ZPassFit.Data.Repositories.Memberships;
using ZPassFit.Dto;
using ZPassFit.Services.Interfaces;

namespace ZPassFit.Services.Implementations;

public class DashboardService(
    IVisitLogRepository visitLogRepository,
    IQrSessionRepository qrSessionRepository,
    IClientRepository clientRepository,
    IMembershipRepository membershipRepository,
    IPaymentRepository paymentRepository
) : IDashboardService
{
    private const int RecentCheckInsTake = 20;

    public async Task<EmployeeDashboardResponse> GetEmployeeDashboardAsync(
        CancellationToken cancellationToken = default
    )
    {
        var utcNow = DateTime.UtcNow;
        var (dayStart, dayEnd) = GetUtcDayRange(utcNow);

        var visitsToday = await visitLogRepository.CountVisitsEnteringBetweenAsync(dayStart, dayEnd);
        var inClub = await visitLogRepository.CountOpenVisitsAsync();
        var qrActive = await qrSessionRepository.CountActiveAsync(utcNow);
        var recentLogs = await visitLogRepository.GetRecentVisitsWithClientAsync(RecentCheckInsTake);

        var recent = recentLogs.Select(MapRecentCheckIn).ToList();

        return new EmployeeDashboardResponse(
            dayStart,
            dayEnd,
            visitsToday,
            inClub,
            qrActive,
            recent
        );
    }

    public async Task<AdminDashboardResponse> GetAdminDashboardAsync(
        CancellationToken cancellationToken = default
    )
    {
        var staff = await GetEmployeeDashboardAsync(cancellationToken);
        var utcNow = DateTime.UtcNow;
        var (dayStart, dayEnd) = GetUtcDayRange(utcNow);

        var totalClients = await clientRepository.CountAsync();
        var activeMemberships = await membershipRepository.CountActiveAsync(utcNow);
        var (paymentsCount, paymentsTotal) =
            await paymentRepository.GetCompletedPaymentsSummaryBetweenAsync(dayStart, dayEnd);

        return new AdminDashboardResponse(
            staff,
            totalClients,
            activeMemberships,
            paymentsCount,
            paymentsTotal
        );
    }

    private static (DateTime Start, DateTime End) GetUtcDayRange(DateTime utcNow)
    {
        var start = utcNow.Date;
        return (start, start.AddDays(1));
    }

    private static RecentCheckInItem MapRecentCheckIn(VisitLog v)
    {
        var c = v.Client;
        var name = $"{c.LastName} {c.FirstName} {c.MiddleName}".Trim();
        return new RecentCheckInItem(v.Id, v.ClientId, name, v.EnterDate);
    }
}
