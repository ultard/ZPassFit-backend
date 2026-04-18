using Microsoft.EntityFrameworkCore;
using ZPassFit.Data.Models.Clients;

namespace ZPassFit.Data.Repositories.Clients;

public class BonusTransactionRepository(ApplicationDbContext context) : IBonusTransactionRepository
{
    public async Task<(IReadOnlyList<BonusTransaction> Items, int TotalCount)> GetPagedAsync(
        DateTime? createFromUtc,
        DateTime? createToUtc,
        Guid? clientId,
        BonusTransactionType? type,
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default
    )
    {
        var query = context.BonusTransactions.AsNoTracking().Include(t => t.Client).AsQueryable();

        if (createFromUtc is { } from)
            query = query.Where(t => t.CreateDate >= from);

        if (createToUtc is { } to)
            query = query.Where(t => t.CreateDate < to);

        if (clientId is { } cid)
            query = query.Where(t => t.ClientId == cid);

        if (type.HasValue)
            query = query.Where(t => t.Type == type.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var pattern = $"%{term}%";
            query = query.Where(t =>
                EF.Functions.ILike(t.Client.LastName, pattern)
                || EF.Functions.ILike(t.Client.FirstName, pattern)
                || EF.Functions.ILike(t.Client.Phone, pattern)
                || EF.Functions.ILike(t.Client.Email, pattern)
            );
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(t => t.CreateDate)
            .ThenByDescending(t => t.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

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