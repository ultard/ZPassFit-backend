namespace ZPassFit.Dashboard;

public class DashboardOptions
{
    public const string SectionName = "Dashboard";

    /// <summary>IANA id, e.g. Europe/Moscow (Linux/macOS and modern Windows).</summary>
    public string TimeZoneId { get; set; } = "Europe/Moscow";
}
