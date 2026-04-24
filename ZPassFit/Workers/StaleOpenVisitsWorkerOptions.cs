namespace ZPassFit.Workers;

public class StaleOpenVisitsWorkerOptions
{
    public const string SectionName = "StaleOpenVisits";

    /// <summary>Как часто запускать проверку (по умолчанию раз в час).</summary>
    public TimeSpan CheckInterval { get; set; } = TimeSpan.FromHours(1);

    /// <summary>После какой длительности открытого визита считать его «зависшим» и закрыть (по умолчанию сутки).</summary>
    public TimeSpan MaxOpenDuration { get; set; } = TimeSpan.FromDays(1);
}
