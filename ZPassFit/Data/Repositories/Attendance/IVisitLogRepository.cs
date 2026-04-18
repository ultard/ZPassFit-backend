using ZPassFit.Data.Models.Attendance;
using ZPassFit.Data.Repositories;

namespace ZPassFit.Data.Repositories.Attendance;

public interface IVisitLogRepository
{
    Task AddAsync(VisitLog visitLog);
    Task UpdateAsync(VisitLog visitLog);
    Task<VisitLog?> GetOpenVisitByClientIdAsync(Guid clientId);
    Task<IEnumerable<VisitLog>> GetVisitHistoryByClientIdAsync(Guid clientId);
    Task<VisitLog?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<int> CountVisitsEnteringBetweenAsync(DateTime fromUtcInclusive, DateTime toUtcExclusive);
    Task<int> CountOpenVisitsAsync();

    Task<IReadOnlyList<ClubDayCountRow>> GetVisitCountsByClubDayAsync(
        DateTime fromUtcInclusive,
        DateTime toUtcExclusive,
        string timeZoneId,
        CancellationToken cancellationToken = default
    );

    Task<(IReadOnlyList<VisitLog> Items, int TotalCount)> GetPagedAsync(
        DateTime? enterFromUtc,
        DateTime? enterToUtc,
        Guid? clientId,
        bool? openOnly,
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default
    );
}