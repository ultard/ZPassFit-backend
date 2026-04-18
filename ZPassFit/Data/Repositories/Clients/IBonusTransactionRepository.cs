using ZPassFit.Data.Models.Clients;

namespace ZPassFit.Data.Repositories.Clients;

public interface IBonusTransactionRepository
{
    Task<(IReadOnlyList<BonusTransaction> Items, int TotalCount)> GetPagedAsync(
        DateTime? createFromUtc,
        DateTime? createToUtc,
        Guid? clientId,
        BonusTransactionType? type,
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default
    );

    Task<BonusTransaction?> GetByIdAsync(int id);
    Task<IEnumerable<BonusTransaction>> GetByClientIdAsync(Guid clientId);
    Task AddAsync(BonusTransaction transaction);
    Task UpdateAsync(BonusTransaction transaction);
    Task DeleteAsync(int id);
}