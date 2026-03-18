using ZPassFit.Data.Models.Clients;
using ZPassFit.Data.Models;

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
    Card
}

public class Payment
{
    public int Id { get; set; }
    public int Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    
    public DateTime? PaymentDate { get; set; }
    public DateTime CreateDate { get; set; } = DateTime.UtcNow;
    
    public Guid ClientId { get; set; }
    public int? EmployeeId { get; set; }

    public Client Client { get; set; } = null!;
    public Employee? Employee { get; set; }
}