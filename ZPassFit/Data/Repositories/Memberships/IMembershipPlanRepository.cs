using ZPassFit.Data.Models.Memberships;

namespace ZPassFit.Data.Repositories.Memberships;

public interface IMembershipPlanRepository
{
    Task<MembershipPlan?> GetByIdAsync(Guid id);
    Task<IEnumerable<MembershipPlan>> GetAllAsync();
    Task AddAsync(MembershipPlan plan);
    Task UpdateAsync(MembershipPlan plan);
    Task DeleteAsync(Guid id);
}