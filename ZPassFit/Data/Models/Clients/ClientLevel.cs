namespace ZPassFit.Data.Models.Clients;

public class ClientLevel
{
    public int Id { get; set; }

    public DateTime ReceiveDate = DateTime.Now;
    public DateTime? RevocationDate;

    public Guid ClientId { get; set; }
    
    public Client Client { get; set; } = null!;
    public Level Level { get; set; } = null!;
}