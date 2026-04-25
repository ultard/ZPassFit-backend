namespace ZPassFit.Workers;

public class ExpiredGraceLevelsWorkerOptions
{
    public const string SectionName = "ExpiredGraceLevels";

    /// <summary>Как часто запускать проверку (по умолчанию раз в час).</summary>
    public TimeSpan CheckInterval { get; set; } = TimeSpan.FromHours(1);
}

