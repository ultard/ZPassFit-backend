using Microsoft.EntityFrameworkCore;
using ZPassFit.Data.Models.Audit;

namespace ZPassFit.Data.Repositories.Audit;

public class AuditLogRepository(ApplicationDbContext context) : IAuditLogRepository
{
    public async Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> GetPagedAsync(
        DateTime? fromUtcInclusive,
        DateTime? toUtcExclusive,
        string? actionContains,
        string? entityTypeContains,
        int skip,
        int take,
        CancellationToken cancellationToken = default
    )
    {
        var query = context.AuditLogs.AsNoTracking().Include(a => a.User).AsQueryable();

        if (fromUtcInclusive.HasValue)
            query = query.Where(a => a.OccurredAtUtc >= fromUtcInclusive.Value);

        if (toUtcExclusive.HasValue)
            query = query.Where(a => a.OccurredAtUtc < toUtcExclusive.Value);

        if (!string.IsNullOrWhiteSpace(actionContains))
        {
            var term = actionContains.Trim();
            var pattern = $"%{term}%";
            query = query.Where(a => EF.Functions.ILike(a.Action, pattern));
        }

        if (!string.IsNullOrWhiteSpace(entityTypeContains))
        {
            var term = entityTypeContains.Trim();
            var pattern = $"%{term}%";
            query = query.Where(a => EF.Functions.ILike(a.EntityType, pattern));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(a => a.OccurredAtUtc)
            .ThenByDescending(a => a.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<AuditLog?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await context.AuditLogs
            .AsNoTracking()
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }
}
