using ZPassFit.Dto;

namespace ZPassFit.Services.Interfaces;

public interface ILevelService
{
    Task<IReadOnlyList<LevelResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<LevelResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<LevelResponse> CreateAsync(CreateLevelRequest request, CancellationToken cancellationToken = default);
    Task<LevelResponse?> UpdateAsync(Guid id, UpdateLevelRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
