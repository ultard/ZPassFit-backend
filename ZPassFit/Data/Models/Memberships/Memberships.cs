using ZPassFit.Data.Models.Clients;

namespace ZPassFit.Data.Models.Memberships;

public enum MembershipStatus
{
    Active,
    Frozen,
    Expired,
    Disabled
}

public class Membership
{
    public int Id { get; set; }

    public MembershipStatus Status { get; set; } = MembershipStatus.Active;
    public DateTime ActivatedDate { get; set; } = DateTime.UtcNow;
    public DateTime ExpireDate { get; set; }

    public int PlanId { get; set; }
    public Guid ClientId { get; set; }
    public Client Client { get; set; } = null!;
    public MembershipPlan Plan { get; set; } = null!;
}