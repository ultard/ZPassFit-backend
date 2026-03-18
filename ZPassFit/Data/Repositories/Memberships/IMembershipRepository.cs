using ZPassFit.Data.Models.Memberships;

namespace ZPassFit.Data.Repositories.Memberships;

public interface IMembershipRepository
{
    Task<Membership?> GetByIdAsync(int id);
    Task<Membership?> GetByClientIdAsync(Guid clientId);
    Task AddAsync(Membership membership);
    Task UpdateAsync(Membership membership);
    Task DeleteAsync(int id);
}