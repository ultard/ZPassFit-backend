using ZPassFit.Data.Models.Clients;

namespace ZPassFit.Data.Repositories.Clients;

public interface ILevelRepository
{
    Task<Level?> GetByIdAsync(int id);
    Task AddAsync(Level level);
    Task UpdateAsync(Level level);
    Task DeleteAsync(int id);
}

