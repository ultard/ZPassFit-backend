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
}