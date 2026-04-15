using ZPassFit.Data.Models.Memberships;

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

/// <summary>
/// Строка журнала посещений для списка в дашборде.
/// </summary>
public record VisitLogListItemResponse(
    int Id,
    DateTime EnterDate,
    DateTime? LeaveDate,
    int MembershipId,
    Guid ClientId,
    string ClientLastName,
    string ClientFirstName
);

public record PagedVisitLogsResponse(
    int Page,
    int PageSize,
    int TotalCount,
    IReadOnlyList<VisitLogListItemResponse> Items
);

/// <summary>
/// Абонемент в списке дашборда с данными клиента и тарифа.
/// </summary>
public record MembershipListItemResponse(
    int Id,
    int PlanId,
    string PlanName,
    Guid ClientId,
    string ClientLastName,
    string ClientFirstName,
    MembershipStatus Status,
    DateTime ActivatedDate,
    DateTime ExpireDate
);

public record PagedMembershipsResponse(
    int Page,
    int PageSize,
    int TotalCount,
    IReadOnlyList<MembershipListItemResponse> Items
);
