using Microsoft.EntityFrameworkCore;
using ZPassFit.Data.Models.Memberships;

namespace ZPassFit.Data.Repositories.Memberships;

public class MembershipPlanRepository(ApplicationDbContext context) : IMembershipPlanRepository
{
    public async Task<MembershipPlan?> GetByIdAsync(Guid id)
    {
        return await context.MembershipPlans.FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<IEnumerable<MembershipPlan>> GetAllAsync()
    {
        return await context.MembershipPlans
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task AddAsync(MembershipPlan plan)
    {
        await context.MembershipPlans.AddAsync(plan);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(MembershipPlan plan)
    {
        context.MembershipPlans.Update(plan);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var plan = await GetByIdAsync(id);
        if (plan == null) return;

        context.MembershipPlans.Remove(plan);
        await context.SaveChangesAsync();
    }
}