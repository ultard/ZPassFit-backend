using ZPassFit.Data.Models.Clients;

namespace ZPassFit.Data.Models.Memberships;

public enum PaymentStatus
{
    Pending,
    Completed,
    Cancelled
}

public enum PaymentMethod
{
    Cash,
    Card,
    Balance
}

public class Payment
{
    public Guid Id { get; set; }
    public int Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    public DateTime? PaymentDate { get; set; }
    public DateTime CreateDate { get; set; } = DateTime.UtcNow;

    public Guid ClientId { get; set; }
    public Guid? EmployeeId { get; set; }

    public Client Client { get; set; } = null!;
    public Employee? Employee { get; set; }
}