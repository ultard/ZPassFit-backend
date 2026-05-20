namespace ZPassFit.Workers;

public class ExpiredGraceLevelsWorkerOptions
{
    public const string SectionName = "ExpiredGraceLevels";
    public TimeSpan CheckInterval { get; set; } = TimeSpan.FromHours(1);
}