using System.ComponentModel.DataAnnotations;

namespace ZPassFit.Data.Models;

public class Employee
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = null!;

    [MaxLength(100)] public required string LastName { get; set; }
    [MaxLength(100)] public required string FirstName { get; set; }
    [MaxLength(100)] public required string MiddleName { get; set; }
    public DateTime BirthDate { get; set; }

    [MaxLength(15)] public required string Phone { get; set; }
    [MaxLength(255)] public required string Email { get; set; }

    public DateTime? HireDate { get; set; }
    public DateTime? FireDate { get; set; }

    [MaxLength(100)] public string? Notes { get; set; }

    public ApplicationUser User { get; set; } = null!;
}