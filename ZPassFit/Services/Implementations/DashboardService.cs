using System.Globalization;
using Microsoft.Extensions.Options;
using ZPassFit.Dashboard;
using ZPassFit.Data.Repositories;
using ZPassFit.Data.Repositories.Attendance;
using ZPassFit.Data.Repositories.Clients;
using ZPassFit.Data.Repositories.Memberships;
using ZPassFit.Dto;
using ZPassFit.Services.Interfaces;

namespace ZPassFit.Services.Implementations;

public class DashboardService(
    IOptions<DashboardOptions> dashboardOptions,
    IVisitLogRepository visitLogRepository,
    IPaymentRepository paymentRepository,
    IClientRepository clientRepository,
    IMembershipRepository membershipRepository
) : IDashboardService
{
    /// <summary>Относительное изменение ниже этого порога считаем «без изменений» для стрелки направления.</summary>
    private const decimal DirectionFlatPercentThreshold = 0.05m;

    public async Task<DashboardOverviewResponse> GetOverviewAsync(
        int? year,
        int? month,
        CancellationToken cancellationToken = default
    )
    {
        var options = dashboardOptions.Value;
        var clubTimeZone = DashboardPeriodCalculator.ResolveTimeZone(options.TimeZoneId);
        var utcNow = DateTime.UtcNow;

        var (selectedYear, selectedMonth) = DashboardPeriodCalculator.ResolveTargetMonth(
            clubTimeZone,
            utcNow,
            year,
            month
        );

        var (selectedMonthStartUtc, selectedMonthEndUtcExclusive) =
            DashboardPeriodCalculator.GetMonthUtcRange(clubTimeZone, selectedYear, selectedMonth);

        var (previousMonthStartUtc, previousMonthEndUtcExclusive) =
            DashboardPeriodCalculator.GetPreviousMonthUtcRange(clubTimeZone, selectedYear, selectedMonth);

        var kpiData = await LoadKpiDataAsync(
            selectedMonthStartUtc,
            selectedMonthEndUtcExclusive,
            previousMonthStartUtc,
            previousMonthEndUtcExclusive,
            cancellationToken
        );

        var kpis = new[]
        {
            BuildKpi("visits", "Визиты", kpiData.VisitsSelectedMonth, kpiData.VisitsPreviousMonth, "visits"),
            BuildKpi(
                "revenue",
                "Выручка",
                kpiData.PaymentsSelectedMonth.TotalAmount,
                kpiData.PaymentsPreviousMonth.TotalAmount,
                "currency"
            ),
            BuildKpi(
                "newClients",
                "Новые клиенты",
                kpiData.NewClientsSelectedMonth,
                kpiData.NewClientsPreviousMonth,
                "count"
            ),
            BuildKpi(
                "newMemberships",
                "Новые абонементы",
                kpiData.MembershipActivationsSelectedMonth,
                kpiData.MembershipActivationsPreviousMonth,
                "count"
            )
        };

        var chartSource = await LoadChartSourceDataAsync(
            selectedMonthStartUtc,
            selectedMonthEndUtcExclusive,
            options.TimeZoneId,
            cancellationToken
        );

        var visitCountByLocalDay = chartSource.VisitCountsByLocalDay.ToDictionary(row => row.Date, row => row.Count);
        var newClientCountByLocalDay = chartSource.NewClientCountsByLocalDay.ToDictionary(
            row => row.Date,
            row => row.Count
        );
        var paymentAmountByLocalDay = chartSource.PaymentAmountsByLocalDay.ToDictionary(
            row => row.Date,
            row => row.TotalAmount
        );
        var membershipActivationsByPlan = chartSource.MembershipActivationsByPlan;

        var russianCulture = CultureInfo.GetCultureInfo("ru-RU");
        var monthNameTitleCase = russianCulture.TextInfo.ToTitleCase(
            russianCulture.DateTimeFormat.GetMonthName(selectedMonth)
        );
        var periodLabel = $"{monthNameTitleCase} {selectedYear}";

        var totalPlanActivations = membershipActivationsByPlan.Sum(row => row.Count);

        var membershipsByPlanChart = membershipActivationsByPlan
            .Select(row => new DashboardMembershipPlanPoint(
                row.PlanId,
                row.PlanName,
                row.Count,
                totalPlanActivations == 0
                    ? 0
                    : Math.Round(row.Count * 100m / totalPlanActivations, 1, MidpointRounding.AwayFromZero)
            ))
            .ToList();

        var series = new DashboardSeries(
            BuildDailyCountSeries(selectedYear, selectedMonth, visitCountByLocalDay),
            BuildDailyRevenueSeries(selectedYear, selectedMonth, paymentAmountByLocalDay),
            BuildDailyCountSeries(selectedYear, selectedMonth, newClientCountByLocalDay),
            membershipsByPlanChart
        );

        var periodMeta = new DashboardPeriodMeta(
            options.TimeZoneId,
            selectedYear,
            selectedMonth,
            periodLabel,
            selectedMonthStartUtc,
            selectedMonthEndUtcExclusive,
            previousMonthStartUtc,
            previousMonthEndUtcExclusive
        );

        return new DashboardOverviewResponse(periodMeta, kpis, series);
    }

    private sealed record DashboardKpiData(
        int VisitsSelectedMonth,
        int VisitsPreviousMonth,
        (int Count, long TotalAmount) PaymentsSelectedMonth,
        (int Count, long TotalAmount) PaymentsPreviousMonth,
        int NewClientsSelectedMonth,
        int NewClientsPreviousMonth,
        int MembershipActivationsSelectedMonth,
        int MembershipActivationsPreviousMonth
    );

    private sealed record DashboardChartSourceData(
        IReadOnlyList<ClubDayCountRow> VisitCountsByLocalDay,
        IReadOnlyList<ClubDayRevenueRow> PaymentAmountsByLocalDay,
        IReadOnlyList<ClubDayCountRow> NewClientCountsByLocalDay,
        IReadOnlyList<MembershipPlanActivationCount> MembershipActivationsByPlan
    );

    private async Task<DashboardKpiData> LoadKpiDataAsync(
        DateTime selectedMonthStartUtc,
        DateTime selectedMonthEndUtcExclusive,
        DateTime previousMonthStartUtc,
        DateTime previousMonthEndUtcExclusive,
        CancellationToken cancellationToken
    )
    {
        var visitsSelected = await visitLogRepository.CountVisitsEnteringBetweenAsync(
            selectedMonthStartUtc,
            selectedMonthEndUtcExclusive
        );
        var visitsPrevious = await visitLogRepository.CountVisitsEnteringBetweenAsync(
            previousMonthStartUtc,
            previousMonthEndUtcExclusive
        );

        var paymentsSelected = await paymentRepository.GetCompletedPaymentsSummaryBetweenAsync(
            selectedMonthStartUtc,
            selectedMonthEndUtcExclusive
        );
        var paymentsPrevious = await paymentRepository.GetCompletedPaymentsSummaryBetweenAsync(
            previousMonthStartUtc,
            previousMonthEndUtcExclusive
        );

        var newClientsSelected = await clientRepository.CountRegisteredBetweenAsync(
            selectedMonthStartUtc,
            selectedMonthEndUtcExclusive,
            cancellationToken
        );
        var newClientsPrevious = await clientRepository.CountRegisteredBetweenAsync(
            previousMonthStartUtc,
            previousMonthEndUtcExclusive,
            cancellationToken
        );

        var membershipsSelected = await membershipRepository.CountActivatedBetweenAsync(
            selectedMonthStartUtc,
            selectedMonthEndUtcExclusive,
            cancellationToken
        );
        var membershipsPrevious = await membershipRepository.CountActivatedBetweenAsync(
            previousMonthStartUtc,
            previousMonthEndUtcExclusive,
            cancellationToken
        );

        return new DashboardKpiData(
            visitsSelected,
            visitsPrevious,
            paymentsSelected,
            paymentsPrevious,
            newClientsSelected,
            newClientsPrevious,
            membershipsSelected,
            membershipsPrevious
        );
    }

    private async Task<DashboardChartSourceData> LoadChartSourceDataAsync(
        DateTime selectedMonthStartUtc,
        DateTime selectedMonthEndUtcExclusive,
        string timeZoneId,
        CancellationToken cancellationToken
    )
    {
        // See note in LoadKpiDataAsync about DbContext concurrency.
        var visitCounts = await visitLogRepository.GetVisitCountsByClubDayAsync(
            selectedMonthStartUtc,
            selectedMonthEndUtcExclusive,
            timeZoneId,
            cancellationToken
        );
        var paymentAmounts = await paymentRepository.GetCompletedPaymentAmountsByClubDayAsync(
            selectedMonthStartUtc,
            selectedMonthEndUtcExclusive,
            timeZoneId,
            cancellationToken
        );
        var newClientCounts = await clientRepository.GetRegistrationCountsByClubDayAsync(
            selectedMonthStartUtc,
            selectedMonthEndUtcExclusive,
            timeZoneId,
            cancellationToken
        );
        var activationsByPlan = await membershipRepository.CountActivationsByPlanBetweenAsync(
            selectedMonthStartUtc,
            selectedMonthEndUtcExclusive,
            cancellationToken
        );

        return new DashboardChartSourceData(
            visitCounts,
            paymentAmounts,
            newClientCounts,
            activationsByPlan
        );
    }

    private static DashboardKpiItem BuildKpi(
        string metricId,
        string title,
        decimal valueCurrentPeriod,
        decimal valuePreviousPeriod,
        string unit
    )
    {
        decimal? changePercent;
        var isGrowthFromZero = false;

        switch (valuePreviousPeriod)
        {
            case 0 when valueCurrentPeriod == 0:
                changePercent = 0;
                break;
            case 0:
                changePercent = null;
                isGrowthFromZero = valueCurrentPeriod > 0;
                break;
            default:
                changePercent = Math.Round(
                    (valueCurrentPeriod - valuePreviousPeriod) / valuePreviousPeriod * 100m,
                    2,
                    MidpointRounding.AwayFromZero
                );
                break;
        }

        var direction = ResolveTrendDirection(changePercent, valuePreviousPeriod, valueCurrentPeriod);

        return new DashboardKpiItem(
            metricId,
            title,
            valueCurrentPeriod,
            valuePreviousPeriod,
            changePercent,
            direction,
            unit,
            isGrowthFromZero
        );
    }

    private static string ResolveTrendDirection(
        decimal? changePercent,
        decimal valuePreviousPeriod,
        decimal valueCurrentPeriod
    )
    {
        if (valuePreviousPeriod == 0 && valueCurrentPeriod == 0)
            return "flat";

        if (valuePreviousPeriod == 0 && valueCurrentPeriod > 0)
            return "up";

        if (changePercent is null)
            return "flat";

        if (changePercent > DirectionFlatPercentThreshold)
            return "up";

        if (changePercent < -DirectionFlatPercentThreshold)
            return "down";

        return "flat";
    }

    private static List<DashboardDayPoint> BuildDailyCountSeries(
        int year,
        int month,
        IReadOnlyDictionary<DateOnly, int> countByClubLocalDay
    )
    {
        return DashboardPeriodCalculator
            .EnumerateDaysInMonth(year, month)
            .Select(day => new DashboardDayPoint(day, countByClubLocalDay.GetValueOrDefault(day)))
            .ToList();
    }

    private static List<DashboardRevenueDayPoint> BuildDailyRevenueSeries(
        int year,
        int month,
        IReadOnlyDictionary<DateOnly, long> amountByClubLocalDay
    )
    {
        return DashboardPeriodCalculator
            .EnumerateDaysInMonth(year, month)
            .Select(day => new DashboardRevenueDayPoint(day, amountByClubLocalDay.GetValueOrDefault(day)))
            .ToList();
    }
}
