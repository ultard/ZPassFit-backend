using System.ComponentModel.DataAnnotations;
using ZPassFit.Data.Models.Clients;

namespace ZPassFit.Data.Models.Attendance;

public class QrSession
{
    [Key] public Guid Token { get; set; }

    public DateTime CreateDate { get; set; } = DateTime.UtcNow;
    public DateTime ExpireDate { get; set; }

    public Guid ClientId { get; set; }
    public Client Client { get; set; } = null!;
}