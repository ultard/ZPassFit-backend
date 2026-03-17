using ZPassFit.Data.Models.Clients;

namespace ZPassFit.Data.Repositories.Clients;

public interface IClientLevelRepository
{
    Task<ClientLevel?> GetByIdAsync(int id);
    Task<ClientLevel?> GetActiveByClientIdAsync(Guid clientId);
    Task AddAsync(ClientLevel clientLevel);
    Task UpdateAsync(ClientLevel clientLevel);
    Task DeleteAsync(int id);
}

