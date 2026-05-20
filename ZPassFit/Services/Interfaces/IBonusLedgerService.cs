using ZPassFit.Data.Models.Attendance;
using ZPassFit.Data.Models.Clients;

namespace ZPassFit.Services.Interfaces;

public interface IBonusLedgerService
{
    /// <summary>Списывает просроченные бонусы (сгорающие баллы).</summary>
    Task<int> BurnExpiredBonusesAsync(Guid clientId, DateTime utcNow);

    /// <summary>Начисляет бонус за дисциплину при завершении посещения.</summary>
    Task<int> TryAccrueDisciplineBonusAsync(Client client, VisitLog visit, DateTime utcNow);
}