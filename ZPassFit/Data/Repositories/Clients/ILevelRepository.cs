using ZPassFit.Data.Models.Clients;

namespace ZPassFit.Data.Repositories.Clients;

public interface ILevelRepository
{
    Task<IReadOnlyList<Level>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Level?> GetByIdAsync(Guid id);
    Task<Level?> GetNextByPreviousLevelIdAsync(Guid currentLevelId, CancellationToken cancellationToken = default);
    Task AddAsync(Level level);
    Task UpdateAsync(Level level);
    Task DeleteAsync(Guid id);
    Task<int> CountClientLevelsUsingLevelAsync(Guid levelId, CancellationToken cancellationToken = default);
    Task<int> CountLevelsWithPreviousPointingToAsync(Guid levelId, CancellationToken cancellationToken = default);
}