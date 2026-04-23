using ZPassFit.Data.Models.Clients;
using ZPassFit.Data.Models.Memberships;

namespace ZPassFit.Data.Models.Attendance;

public class VisitLog
{
    public Guid Id { get; set; }

    public DateTime EnterDate { get; set; } = DateTime.UtcNow;
    public DateTime? LeaveDate { get; set; }

    public Guid MembershipId { get; set; }
    public Guid ClientId { get; set; }

    public Membership Membership { get; set; } = null!;
    public Client Client { get; set; } = null!;
}