using ZPassFit.Dashboard;

namespace ZPassFit.Test;

public class DashboardPeriodCalculatorTests
{
    private static readonly TimeZoneInfo Moscow = TimeZoneInfo.FindSystemTimeZoneById("Europe/Moscow");

    [Fact]
    public void GetMonthUtcRange_April2026_Moscow_StartsAtMarch31Utc()
    {
        var (from, to) = DashboardPeriodCalculator.GetMonthUtcRange(Moscow, 2026, 4);

        Assert.Equal(DateTimeKind.Utc, from.Kind);
        Assert.Equal(DateTimeKind.Utc, to.Kind);
        Assert.Equal(new DateTime(2026, 3, 31, 21, 0, 0, DateTimeKind.Utc), from);
        Assert.Equal(new DateTime(2026, 4, 30, 21, 0, 0, DateTimeKind.Utc), to);
    }

    [Fact]
    public void GetPreviousMonthUtcRange_AfterJanuary_IsDecember()
    {
        var (from, to) = DashboardPeriodCalculator.GetPreviousMonthUtcRange(Moscow, 2026, 1);

        var (expectedFrom, expectedTo) = DashboardPeriodCalculator.GetMonthUtcRange(Moscow, 2025, 12);
        Assert.Equal(expectedFrom, from);
        Assert.Equal(expectedTo, to);
    }

    [Fact]
    public void ResolveTargetMonth_BothNull_UsesUtcNowInZone()
    {
        var utc = new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var (y, m) = DashboardPeriodCalculator.ResolveTargetMonth(Moscow, utc, null, null);
        Assert.Equal(2026, y);
        Assert.Equal(6, m);
    }

    [Fact]
    public void ResolveTargetMonth_ExplicitYearMonth_ReturnsSame()
    {
        var (y, m) = DashboardPeriodCalculator.ResolveTargetMonth(Moscow, DateTime.UtcNow, 2025, 3);
        Assert.Equal(2025, y);
        Assert.Equal(3, m);
    }

    [Fact]
    public void ResolveTargetMonth_OnlyYear_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            DashboardPeriodCalculator.ResolveTargetMonth(Moscow, DateTime.UtcNow, 2026, null));
    }

    [Fact]
    public void EnumerateDaysInMonth_February2024_Has29Days()
    {
        var days = DashboardPeriodCalculator.EnumerateDaysInMonth(2024, 2).ToList();
        Assert.Equal(29, days.Count);
        Assert.Equal(new DateOnly(2024, 2, 1), days[0]);
        Assert.Equal(new DateOnly(2024, 2, 29), days[^1]);
    }
}
