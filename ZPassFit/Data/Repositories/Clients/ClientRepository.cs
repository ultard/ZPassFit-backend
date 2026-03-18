using Microsoft.EntityFrameworkCore;
using ZPassFit.Data.Models.Clients;

namespace ZPassFit.Data.Repositories.Clients;

public class ClientRepository(ApplicationDbContext context) : IClientRepository
{
    public async Task<Client?> GetByIdAsync(Guid id)
    {
        return await context.Clients
            .Include(c => c.User)
            .Include(c => c.Level)
            .Include(c => c.Membership)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Client?> GetByUserIdAsync(string userId)
    {
        return await context.Clients
            .Include(c => c.Level)
            .Include(c => c.Membership)
            .FirstOrDefaultAsync(c => c.UserId == userId);
    }

    public async Task AddAsync(Client client)
    {
        await context.Clients.AddAsync(client);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Client client)
    {
        context.Clients.Update(client);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var client = await GetByIdAsync(id);
        if (client == null) return;

        context.Clients.Remove(client);
        await context.SaveChangesAsync();
    }
}