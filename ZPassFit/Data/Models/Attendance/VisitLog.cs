using ZPassFit.Data.Models.Clients;
using ZPassFit.Data.Models.Memberships;

namespace ZPassFit.Data.Models.Attendance;

public class VisitLog
{
    public int Id { get; set; }

    public DateTime EnterDate { get; set; } = DateTime.UtcNow;
    public DateTime? LeaveDate { get; set; }

    public int MembershipId { get; set; }
    public Guid ClientId { get; set; }

    public Membership Membership { get; set; } = null!;
    public Client Client { get; set; } = null!;
}