using ZPassFit.Data.Models.Clients;
using ZPassFit.Data.Repositories.Clients;
using ZPassFit.Dto;
using ZPassFit.Services.Interfaces;

namespace ZPassFit.Services.Implementations;

public class LevelService(ILevelRepository levelRepository) : ILevelService
{
    public async Task<IReadOnlyList<LevelResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var levels = await levelRepository.GetAllAsync(cancellationToken);
        return levels.Select(Map).ToList();
    }

    public async Task<LevelResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var level = await levelRepository.GetByIdAsync(id);
        return level == null ? null : Map(level);
    }

    public async Task<LevelResponse> CreateAsync(CreateLevelRequest request, CancellationToken cancellationToken = default)
    {
        ValidateName(request.Name);

        if (request.PreviousLevelId is { } prevId)
        {
            var prev = await levelRepository.GetByIdAsync(prevId);
            if (prev == null)
                throw new InvalidOperationException("Previous level not found.");
        }

        var level = new Level
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            ActivateDays = request.ActivateDays,
            GraceDays = request.GraceDays,
            PreviousLevelId = request.PreviousLevelId
        };

        await levelRepository.AddAsync(level);

        var reloaded = await levelRepository.GetByIdAsync(level.Id)
                       ?? throw new InvalidOperationException("Level not found after save.");
        return Map(reloaded);
    }

    public async Task<LevelResponse?> UpdateAsync(Guid id, UpdateLevelRequest request, CancellationToken cancellationToken = default)
    {
        ValidateName(request.Name);

        var level = await levelRepository.GetByIdAsync(id);
        if (level == null) return null;

        if (request.PreviousLevelId is { } prevId)
        {
            var prev = await levelRepository.GetByIdAsync(prevId);
            if (prev == null)
                throw new InvalidOperationException("Previous level not found.");
        }

        await ValidatePreviousChainAsync(id, request.PreviousLevelId, cancellationToken);

        level.Name = request.Name.Trim();
        level.ActivateDays = request.ActivateDays;
        level.GraceDays = request.GraceDays;
        level.PreviousLevelId = request.PreviousLevelId;

        await levelRepository.UpdateAsync(level);

        var reloaded = await levelRepository.GetByIdAsync(id);
        return reloaded == null ? null : Map(reloaded);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var level = await levelRepository.GetByIdAsync(id);
        if (level == null)
            throw new InvalidOperationException("Level not found.");

        if (await levelRepository.CountClientLevelsUsingLevelAsync(id, cancellationToken) > 0)
            throw new InvalidOperationException("Cannot delete a level assigned to clients.");

        if (await levelRepository.CountLevelsWithPreviousPointingToAsync(id, cancellationToken) > 0)
            throw new InvalidOperationException("Cannot delete a level that is set as previous for another level.");

        await levelRepository.DeleteAsync(id);
    }

    private async Task ValidatePreviousChainAsync(Guid levelId, Guid? newPreviousId, CancellationToken cancellationToken)
    {
        if (newPreviousId == null) return;
        if (newPreviousId == levelId)
            throw new InvalidOperationException("A level cannot reference itself as previous.");

        Guid? p = newPreviousId;
        var visited = new HashSet<Guid>();
        while (p != null)
        {
            if (p == levelId)
                throw new InvalidOperationException("Previous level chain would create a cycle.");

            if (!visited.Add(p.Value))
                break;

            var next = await levelRepository.GetByIdAsync(p.Value);
            if (next == null) break;
            p = next.PreviousLevelId;
        }
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Level name is required.");
    }

    private static LevelResponse Map(Level l)
    {
        return new LevelResponse(
            l.Id,
            l.Name,
            l.ActivateDays,
            l.GraceDays,
            l.PreviousLevelId,
            l.PreviousLevel?.Name
        );
    }
}
