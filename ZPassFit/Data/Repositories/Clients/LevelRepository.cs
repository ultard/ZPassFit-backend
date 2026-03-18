using Microsoft.EntityFrameworkCore;
using ZPassFit.Data.Models.Clients;

namespace ZPassFit.Data.Repositories.Clients;

public class LevelRepository(ApplicationDbContext context) : ILevelRepository
{
    public async Task<Level?> GetByIdAsync(int id)
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

    public async Task DeleteAsync(int id)
    {
        var level = await GetByIdAsync(id);
        if (level == null) return;

        context.Levels.Remove(level);
        await context.SaveChangesAsync();
    }
}