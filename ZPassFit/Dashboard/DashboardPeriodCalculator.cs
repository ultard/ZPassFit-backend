namespace ZPassFit.Dashboard;

public static class DashboardPeriodCalculator
{
    public static TimeZoneInfo ResolveTimeZone(string timeZoneId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            throw new InvalidOperationException(
                $"Dashboard:TimeZoneId '{timeZoneId}' is not a valid time zone id.",
                ex
            );
        }
    }

    /// <summary>
    /// Возвращает календарный год и месяц в <paramref name="clubTimeZone"/> для «сейчас»,
    /// либо проверяет пару query-параметров year/month.
    /// </summary>
    public static (int Year, int Month) ResolveTargetMonth(
        TimeZoneInfo clubTimeZone,
        DateTime utcNow,
        int? queryYear,
        int? queryMonth
    )
    {
        if (queryYear is null && queryMonth is null)
        {
            var nowInClubTimeZone = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.SpecifyKind(utcNow, DateTimeKind.Utc),
                clubTimeZone
            );
            return (nowInClubTimeZone.Year, nowInClubTimeZone.Month);
        }

        if (queryYear is null || queryMonth is null)
            throw new ArgumentException("Specify both year and month, or neither.");

        if (queryMonth is < 1 or > 12)
            throw new ArgumentOutOfRangeException(nameof(queryMonth), "Month must be between 1 and 12.");

        if (queryYear is < 2000 or > 2100)
            throw new ArgumentOutOfRangeException(nameof(queryYear), "Year must be between 2000 and 2100.");

        return (queryYear.Value, queryMonth.Value);
    }

    /// <summary>
    /// Полуночь первого дня месяца по локальному времени клуба до полуночи первого дня следующего месяца — в виде UTC-инстантов.
    /// </summary>
    public static (DateTime FromUtcInclusive, DateTime ToUtcExclusive) GetMonthUtcRange(
        TimeZoneInfo clubTimeZone,
        int year,
        int month
    )
    {
        var monthStartLocal = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Unspecified);
        var nextMonthStartLocal = monthStartLocal.AddMonths(1);

        var fromUtcInclusive = TimeZoneInfo.ConvertTimeToUtc(monthStartLocal, clubTimeZone);
        var toUtcExclusive = TimeZoneInfo.ConvertTimeToUtc(nextMonthStartLocal, clubTimeZone);

        return (fromUtcInclusive, toUtcExclusive);
    }

    public static (DateTime FromUtcInclusive, DateTime ToUtcExclusive) GetPreviousMonthUtcRange(
        TimeZoneInfo clubTimeZone,
        int year,
        int month
    )
    {
        var firstDayOfSelectedMonth = new DateTime(year, month, 1);
        var firstDayOfPreviousMonth = firstDayOfSelectedMonth.AddMonths(-1);

        return GetMonthUtcRange(clubTimeZone, firstDayOfPreviousMonth.Year, firstDayOfPreviousMonth.Month);
    }

    public static IEnumerable<DateOnly> EnumerateDaysInMonth(int year, int month)
    {
        var firstDay = new DateOnly(year, month, 1);
        var firstDayOfNextMonth = firstDay.AddMonths(1);

        for (var day = firstDay; day < firstDayOfNextMonth; day = day.AddDays(1))
            yield return day;
    }
}
