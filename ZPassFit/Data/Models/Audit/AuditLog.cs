using System.ComponentModel.DataAnnotations;

namespace ZPassFit.Data.Models.Audit;

public class AuditLog
{
    public long Id { get; set; }

    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;

    public string? UserId { get; set; }

    [MaxLength(64)]
    public string Action { get; set; } = string.Empty;

    [MaxLength(256)]
    public string EntityType { get; set; } = string.Empty;

    [MaxLength(128)]
    public string? EntityId { get; set; }

    public string? Details { get; set; }

    [MaxLength(45)]
    public string? IpAddress { get; set; }

    public ApplicationUser? User { get; set; }
}
