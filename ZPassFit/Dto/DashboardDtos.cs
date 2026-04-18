namespace ZPassFit.Dto;

public record DashboardOverviewResponse(
    DashboardPeriodMeta Period,
    IReadOnlyList<DashboardKpiItem> Kpis,
    DashboardSeries Series
);

public record DashboardPeriodMeta(
    string TimeZoneId,
    int Year,
    int Month,
    string Label,
    DateTime PeriodFromUtc,
    DateTime PeriodToUtcExclusive,
    DateTime CompareFromUtc,
    DateTime CompareToUtcExclusive
);

public record DashboardKpiItem(
    string Id,
    string Title,
    decimal Value,
    decimal PreviousValue,
    decimal? ChangePercent,
    string Direction,
    string Unit,
    bool IsNewGrowth
);

public record DashboardSeries(
    IReadOnlyList<DashboardDayPoint> VisitsByDay,
    IReadOnlyList<DashboardRevenueDayPoint> RevenueByDay,
    IReadOnlyList<DashboardDayPoint> NewClientsByDay,
    IReadOnlyList<DashboardMembershipPlanPoint> MembershipsByPlan
);

public record DashboardDayPoint(DateOnly Date, int Value);

public record DashboardRevenueDayPoint(DateOnly Date, long Amount);

public record DashboardMembershipPlanPoint(int PlanId, string PlanName, int Count, decimal SharePercent);
