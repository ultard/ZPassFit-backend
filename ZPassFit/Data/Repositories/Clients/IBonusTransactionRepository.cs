using ZPassFit.Data.Models.Clients;

namespace ZPassFit.Data.Repositories.Clients;

public interface IBonusTransactionRepository
{
    Task<BonusTransaction?> GetByIdAsync(int id);
    Task<IEnumerable<BonusTransaction>> GetByClientIdAsync(Guid clientId);
    Task AddAsync(BonusTransaction transaction);
    Task UpdateAsync(BonusTransaction transaction);
    Task DeleteAsync(int id);
}