namespace ZPassFit.Data.Models.Clients;

public class ClientLevel
{
    public DateTime ReceiveDate = DateTime.UtcNow;
    public DateTime? RevocationDate;
    public Guid Id { get; set; }

    public Guid ClientId { get; set; }
    public Guid LevelId { get; set; }

    public Client Client { get; set; } = null!;
    public Level Level { get; set; } = null!;
}