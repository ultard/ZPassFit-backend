using Microsoft.EntityFrameworkCore;
using ZPassFit.Data.Models.Clients;

namespace ZPassFit.Data.Repositories.Clients;

public class LevelRepository(ApplicationDbContext context) : ILevelRepository
{
    public async Task<IReadOnlyList<Level>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await context.Levels
            .AsNoTracking()
            .Include(l => l.PreviousLevel)
            .OrderBy(l => l.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<Level?> GetByIdAsync(Guid id)
    {
        return await context.Levels
            .Include(l => l.PreviousLevel)
            .FirstOrDefaultAsync(l => l.Id == id);
    }

    public async Task AddAsync(Level level)
    {
        await context.Levels.AddAsync(level);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Level level)
    {
        context.Levels.Update(level);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var level = await GetByIdAsync(id);
        if (level == null) return;

        context.Levels.Remove(level);
        await context.SaveChangesAsync();
    }

    public async Task<int> CountClientLevelsUsingLevelAsync(Guid levelId, CancellationToken cancellationToken = default)
    {
        return await context.ClientLevels.CountAsync(cl => cl.LevelId == levelId, cancellationToken);
    }

    public async Task<int> CountLevelsWithPreviousPointingToAsync(
        Guid levelId,
        CancellationToken cancellationToken = default
    )
    {
        return await context.Levels.CountAsync(l => l.PreviousLevelId == levelId, cancellationToken);
    }
}