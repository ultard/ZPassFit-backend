using ZPassFit.Data.Models.Attendance;
using ZPassFit.Data.Models.Clients;

namespace ZPassFit.Services.Interfaces;

public interface IBonusLedgerService
{
    Task<int> BurnExpiredBonusesAsync(Guid clientId, DateTime utcNow);
    Task<int> TryAccrueDisciplineBonusAsync(Client client, VisitLog visit, DateTime utcNow);
}