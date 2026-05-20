using ZPassFit.Options.Dashboard;

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
    public void ResolveTargetMonth_OnlyYear_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            DashboardPeriodCalculator.ResolveTargetMonth(Moscow, DateTime.UtcNow, 2026, null));
    }
}