using ZPassFit.Data.Models.Memberships;

namespace ZPassFit.Data.Repositories.Memberships;

public interface IMembershipRepository
{
    Task<Membership?> GetByIdAsync(int id);
    Task<Membership?> GetByClientIdAsync(Guid clientId);
    Task AddAsync(Membership membership);
    Task UpdateAsync(Membership membership);
    Task DeleteAsync(int id);
    Task<int> CountActiveAsync(DateTime utcNow);
    Task<int> CountByPlanIdAsync(int planId, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Membership> Items, int TotalCount)> GetPagedAsync(
        MembershipStatus? status,
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default
    );
}