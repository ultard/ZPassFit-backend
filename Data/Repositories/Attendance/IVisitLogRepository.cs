using ZPassFit.Data.Models.Attendance;

namespace ZPassFit.Data.Repositories.Attendance;

public interface IVisitLogRepository
{
    Task AddAsync(VisitLog visitLog);
    Task UpdateAsync(VisitLog visitLog);
    Task<VisitLog?> GetOpenVisitByClientIdAsync(Guid clientId);
    Task<IEnumerable<VisitLog>> GetVisitHistoryByClientIdAsync(Guid clientId);
}