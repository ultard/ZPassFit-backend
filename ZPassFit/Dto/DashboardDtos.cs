namespace ZPassFit.Dto;

public record RecentCheckInItem(
    int VisitId,
    Guid ClientId,
    string ClientFullName,
    DateTime EnterDateUtc
);

/// <summary>
/// Оперативные показатели для сотрудников (ресепшен) и администратора.
/// </summary>
public record EmployeeDashboardResponse(
    DateTime TodayUtcStart,
    DateTime TodayUtcEnd,
    int VisitsTodayCount,
    int ClientsInClubNow,
    int ActiveQrSessionsCount,
    IReadOnlyList<RecentCheckInItem> RecentCheckIns
);

/// <summary>
/// Расширенная сводка для администратора.
/// </summary>
public record AdminDashboardResponse(
    EmployeeDashboardResponse Staff,
    int TotalClients,
    int ActiveMemberships,
    int PaymentsTodayCount,
    long PaymentsTodayTotalAmount
);
