namespace ZPassFit.Data.Models.Clients;

public enum BonusTransactionType
{
    Accrual,
    Redeem,
    Expire,
    Adjust
}

public class BonusTransaction
{
    public Guid Id { get; set; }

    public BonusTransactionType Type { get; set; }
    public int Amount { get; set; }
    public DateTime CreateDate { get; set; } = DateTime.UtcNow;
    public DateTime? ExpireDate { get; set; }

    /// <summary>Посещение, за которое начислен бонус (дисциплина).</summary>
    public Guid? VisitLogId { get; set; }

    /// <summary>Для Expire — ссылка на исходное начисление Accrual.</summary>
    public Guid? RelatedTransactionId { get; set; }

    public Guid ClientId { get; set; }
    public Client Client { get; set; } = null!;
}