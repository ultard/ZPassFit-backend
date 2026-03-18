namespace ZPassFit.Data.Models.Clients;

public class ClientLevel
{
    public DateTime ReceiveDate = DateTime.Now;
    public DateTime? RevocationDate;
    public int Id { get; set; }

    public Guid ClientId { get; set; }

    public Client Client { get; set; } = null!;
    public Level Level { get; set; } = null!;
}