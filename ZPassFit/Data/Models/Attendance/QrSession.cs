using System.ComponentModel.DataAnnotations;
using ZPassFit.Data.Models.Clients;

namespace ZPassFit.Data.Models.Attendance;

public class QrSession
{
    [Key] [MaxLength(255)] public required string Token { get; set; }

    public DateTime CreateDate { get; set; }
    public DateTime ExpireDate { get; set; }

    public Guid ClientId { get; set; }
    public Client Client { get; set; } = null!;
}