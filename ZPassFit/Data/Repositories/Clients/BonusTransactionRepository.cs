using Microsoft.EntityFrameworkCore;
using ZPassFit.Data.Models.Clients;

namespace ZPassFit.Data.Repositories.Clients;

public class BonusTransactionRepository(ApplicationDbContext context) : IBonusTransactionRepository
{
    public async Task<BonusTransaction?> GetByIdAsync(int id)
    {
        return await context.BonusTransactions
            .Include(t => t.Client)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<IEnumerable<BonusTransaction>> GetByClientIdAsync(Guid clientId)
    {
        return await context.BonusTransactions
            .Where(t => t.ClientId == clientId)
            .OrderByDescending(t => t.CreateDate)
            .ToListAsync();
    }

    public async Task AddAsync(BonusTransaction transaction)
    {
        await context.BonusTransactions.AddAsync(transaction);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(BonusTransaction transaction)
    {
        context.BonusTransactions.Update(transaction);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var transaction = await GetByIdAsync(id);
        if (transaction == null) return;

        context.BonusTransactions.Remove(transaction);
        await context.SaveChangesAsync();
    }
}