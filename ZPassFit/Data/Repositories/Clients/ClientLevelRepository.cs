using Microsoft.EntityFrameworkCore;
using ZPassFit.Data.Models.Clients;

namespace ZPassFit.Data.Repositories.Clients;

public class ClientLevelRepository(ApplicationDbContext context) : IClientLevelRepository
{
    public async Task<ClientLevel?> GetByIdAsync(int id)
    {
        return await context.ClientLevels
            .Include(cl => cl.Client)
            .Include(cl => cl.Level)
            .FirstOrDefaultAsync(cl => cl.Id == id);
    }

    public async Task<ClientLevel?> GetActiveByClientIdAsync(Guid clientId)
    {
        return await context.ClientLevels
            .Include(cl => cl.Level)
            .ThenInclude(l => l.PreviousLevel)
            .FirstOrDefaultAsync(cl => cl.ClientId == clientId && cl.RevocationDate == null);
    }

    public async Task AddAsync(ClientLevel clientLevel)
    {
        await context.ClientLevels.AddAsync(clientLevel);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(ClientLevel clientLevel)
    {
        context.ClientLevels.Update(clientLevel);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var clientLevel = await GetByIdAsync(id);
        if (clientLevel == null) return;

        context.ClientLevels.Remove(clientLevel);
        await context.SaveChangesAsync();
    }
}