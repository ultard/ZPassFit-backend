using Microsoft.EntityFrameworkCore;
using ZPassFit.Data.Models.Memberships;

namespace ZPassFit.Data.Repositories.Memberships;

public class MembershipRepository(ApplicationDbContext context) : IMembershipRepository
{
    public async Task<Membership?> GetByIdAsync(int id)
    {
        return await context.Memberships
            .Include(m => m.Plan)
            .Include(m => m.Client)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<Membership?> GetByClientIdAsync(Guid clientId)
    {
        return await context.Memberships
            .Include(m => m.Plan)
            .FirstOrDefaultAsync(m => m.ClientId == clientId);
    }

    public async Task AddAsync(Membership membership)
    {
        await context.Memberships.AddAsync(membership);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Membership membership)
    {
        context.Memberships.Update(membership);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var membership = await GetByIdAsync(id);
        if (membership == null) return;

        context.Memberships.Remove(membership);
        await context.SaveChangesAsync();
    }

    public async Task<int> CountActiveAsync(DateTime utcNow)
    {
        return await context.Memberships.CountAsync(m =>
            m.Status == MembershipStatus.Active && m.ExpireDate > utcNow
        );
    }

    public async Task<int> CountByPlanIdAsync(int planId, CancellationToken cancellationToken = default)
    {
        return await context.Memberships.CountAsync(m => m.PlanId == planId, cancellationToken);
    }

    public async Task<(IReadOnlyList<Membership> Items, int TotalCount)> GetPagedAsync(
        MembershipStatus? status,
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default
    )
    {
        var query = context.Memberships.AsNoTracking().Include(m => m.Plan).Include(m => m.Client).AsQueryable();

        if (status is { } s)
            query = query.Where(m => m.Status == s);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var pattern = $"%{term}%";
            query = query.Where(m =>
                EF.Functions.ILike(m.Client.LastName, pattern)
                || EF.Functions.ILike(m.Client.FirstName, pattern)
                || EF.Functions.ILike(m.Client.MiddleName, pattern)
                || EF.Functions.ILike(m.Client.Phone, pattern)
                || EF.Functions.ILike(m.Client.Email, pattern)
                || EF.Functions.ILike(m.Plan.Name, pattern)
            );
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(m => m.ActivatedDate)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}