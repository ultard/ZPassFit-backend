namespace ZPassFit.Workers;

public class StaleOpenVisitsWorkerOptions
{
    public const string SectionName = "StaleOpenVisits";
    public TimeSpan CheckInterval { get; set; } = TimeSpan.FromHours(1);
    public TimeSpan MaxOpenDuration { get; set; } = TimeSpan.FromDays(1);
}