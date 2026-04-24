using ZPassFit.Data.Models.Attendance;
using ZPassFit.Data.Repositories;

namespace ZPassFit.Data.Repositories.Attendance;

public interface IVisitLogRepository
{
    Task AddAsync(VisitLog visitLog);
    Task UpdateAsync(VisitLog visitLog);
    Task<VisitLog?> GetOpenVisitByClientIdAsync(Guid clientId);
    Task<IEnumerable<VisitLog>> GetVisitHistoryByClientIdAsync(Guid clientId);
    Task<int> CountDistinctVisitDaysByClientAsync(
        Guid clientId,
        DateTime fromUtcInclusive,
        CancellationToken cancellationToken = default);
    Task<VisitLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

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

    /// <summary>
    /// Закрывает открытые посещения, у которых <see cref="VisitLog.EnterDate"/> раньше чем (UTC сейчас − <paramref name="maxOpenDuration"/>).
    /// <see cref="VisitLog.LeaveDate"/> выставляется как <see cref="VisitLog.EnterDate"/> + <paramref name="maxOpenDuration"/>.
    /// </summary>
    Task<int> AutoCloseStaleOpenVisitsAsync(TimeSpan maxOpenDuration, CancellationToken cancellationToken = default);
}