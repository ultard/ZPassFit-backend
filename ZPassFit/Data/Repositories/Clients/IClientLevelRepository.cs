using ZPassFit.Data.Models.Clients;

namespace ZPassFit.Data.Repositories.Clients;

public interface IClientLevelRepository
{
    Task<ClientLevel?> GetByIdAsync(Guid id);
    Task<ClientLevel?> GetActiveByClientIdAsync(Guid clientId);
    Task AddAsync(ClientLevel clientLevel);
    Task UpdateAsync(ClientLevel clientLevel);
    Task DeleteAsync(Guid id);
    Task<int> ResetLevelsWithExpiredGraceAsync(CancellationToken cancellationToken = default);
}