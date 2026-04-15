using ZPassFit.Data.Models.Attendance;

namespace ZPassFit.Data.Repositories.Attendance;

public interface IVisitLogRepository
{
    Task AddAsync(VisitLog visitLog);
    Task UpdateAsync(VisitLog visitLog);
    Task<VisitLog?> GetOpenVisitByClientIdAsync(Guid clientId);
    Task<IEnumerable<VisitLog>> GetVisitHistoryByClientIdAsync(Guid clientId);

    Task<int> CountVisitsEnteringBetweenAsync(DateTime fromUtcInclusive, DateTime toUtcExclusive);
    Task<int> CountOpenVisitsAsync();
    Task<IReadOnlyList<VisitLog>> GetRecentVisitsWithClientAsync(int take);
}