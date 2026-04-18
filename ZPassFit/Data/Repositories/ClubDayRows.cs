namespace ZPassFit.Data.Repositories;

/// <summary>Maps SQL rows for day-bucketed counts in the club time zone.</summary>
public sealed class ClubDayCountRow
{
    public DateOnly Date { get; set; }
    public int Count { get; set; }
}

/// <summary>Maps SQL rows for completed payment amounts per club-local day.</summary>
public sealed class ClubDayRevenueRow
{
    public DateOnly Date { get; set; }
    public long TotalAmount { get; set; }
}
