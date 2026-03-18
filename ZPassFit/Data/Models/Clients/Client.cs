using System.ComponentModel.DataAnnotations;
using ZPassFit.Data.Models.Memberships;

namespace ZPassFit.Data.Models.Clients;

public enum ClientStatus
{
    Pending,
    Active,
    Blocked
}

public enum ClientGender
{
    Male,
    Female,
    Unknown
}

public class Client
{
    public ICollection<BonusTransaction> BonusTransactions = [];
    public Guid Id { get; set; }
    public string UserId { get; set; } = null!;

    [MaxLength(100)] public required string LastName { get; set; }
    [MaxLength(100)] public required string FirstName { get; set; }
    [MaxLength(100)] public required string MiddleName { get; set; }
    public DateTime BirthDate { get; set; }
    public ClientGender Gender { get; set; } = ClientGender.Unknown;

    [MaxLength(15)] public required string Phone { get; set; }
    [MaxLength(255)] public required string Email { get; set; }

    public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;
    public ClientStatus Status { get; set; } = ClientStatus.Active;

    [Range(0, int.MaxValue)] public int Bonuses { get; set; }

    [MaxLength(100)] public string? Notes { get; set; }

    public ApplicationUser User { get; set; } = null!;
    public ClientLevel? Level { get; set; }

    public Membership? Membership { get; set; }
    public ICollection<Payment> Payments { get; set; } = [];
}