using System.ComponentModel.DataAnnotations;

namespace ZPassFit.Data.Models.Memberships;

public class MembershipPlan
{
    public Guid Id { get; set; }

    [MaxLength(32)] public required string Name { get; set; }

    [MaxLength(128)] public required string Description { get; set; }

    public int[] Durations { get; set; } = [];
    public int Price { get; set; }
}