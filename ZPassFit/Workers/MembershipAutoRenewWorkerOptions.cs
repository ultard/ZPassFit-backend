namespace ZPassFit.Workers;

public class MembershipAutoRenewWorkerOptions
{
    public const string SectionName = "MembershipAutoRenew";
    public TimeSpan CheckInterval { get; set; } = TimeSpan.FromHours(1);
    public int MaxRenewalsPerMembership { get; set; } = 12;
}