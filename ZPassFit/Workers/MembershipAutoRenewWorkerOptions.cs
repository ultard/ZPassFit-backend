namespace ZPassFit.Workers;

public class MembershipAutoRenewWorkerOptions
{
    public const string SectionName = "MembershipAutoRenew";

    /// <summary>Как часто проверять абонементы на продление (по умолчанию раз в час).</summary>
    public TimeSpan CheckInterval { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Максимум продлений за один проход на один абонемент.
    /// Это защищает от долгих простоев воркера, когда подписка успела устареть на много периодов.
    /// </summary>
    public int MaxRenewalsPerMembership { get; set; } = 12;
}

