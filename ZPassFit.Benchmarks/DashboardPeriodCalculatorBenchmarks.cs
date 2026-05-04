using BenchmarkDotNet.Attributes;
using ZPassFit.Dashboard;

namespace ZPassFit.Benchmarks;

[MemoryDiagnoser]
public class DashboardPeriodCalculatorBenchmarks
{
    private TimeZoneInfo _tz = null!;

    [GlobalSetup]
    public void Setup() => _tz = TimeZoneInfo.FindSystemTimeZoneById("Europe/Moscow");

    [Benchmark]
    public (DateTime From, DateTime To) GetMonthUtcRange() =>
        DashboardPeriodCalculator.GetMonthUtcRange(_tz, 2026, 6);

    [Benchmark]
    public (DateTime From, DateTime To) GetPreviousMonthUtcRange() =>
        DashboardPeriodCalculator.GetPreviousMonthUtcRange(_tz, 2026, 1);

    [Benchmark]
    public (int Year, int Month) ResolveTargetMonth_Query() =>
        DashboardPeriodCalculator.ResolveTargetMonth(_tz, DateTime.UtcNow, 2026, 4);

    [Benchmark]
    public (int Year, int Month) ResolveTargetMonth_Now() =>
        DashboardPeriodCalculator.ResolveTargetMonth(_tz, new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc), null, null);

    [Benchmark]
    public int EnumerateDaysInMonth_Count() => DashboardPeriodCalculator.EnumerateDaysInMonth(2026, 6).Count();
}
