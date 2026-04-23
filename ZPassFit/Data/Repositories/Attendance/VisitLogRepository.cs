using Microsoft.EntityFrameworkCore;
using ZPassFit.Data.Models.Attendance;
using ZPassFit.Data.Repositories;

namespace ZPassFit.Data.Repositories.Attendance;

public class VisitLogRepository(ApplicationDbContext context) : IVisitLogRepository
{
    public async Task AddAsync(VisitLog visitLog)
    {
        await context.VisitLogs.AddAsync(visitLog);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(VisitLog visitLog)
    {
        context.VisitLogs.Update(visitLog);
        await context.SaveChangesAsync();
    }

    public async Task<VisitLog?> GetOpenVisitByClientIdAsync(Guid clientId)
    {
        return await context.VisitLogs
            .FirstOrDefaultAsync(v =>
                v.ClientId == clientId &&
                v.LeaveDate == null);
    }

    public async Task<IEnumerable<VisitLog>> GetVisitHistoryByClientIdAsync(Guid clientId)
    {
        return await context.VisitLogs
            .Where(v => v.ClientId == clientId)
            .OrderByDescending(v => v.EnterDate)
            .ToListAsync();
    }

    public async Task<int> CountVisitsEnteringBetweenAsync(
        DateTime fromUtcInclusive,
        DateTime toUtcExclusive
    )
    {
        return await context.VisitLogs.CountAsync(v =>
            v.EnterDate >= fromUtcInclusive && v.EnterDate < toUtcExclusive
        );
    }

    public async Task<int> CountOpenVisitsAsync()
    {
        return await context.VisitLogs.CountAsync(v => v.LeaveDate == null);
    }

    public async Task<IReadOnlyList<ClubDayCountRow>> GetVisitCountsByClubDayAsync(
        DateTime fromUtcInclusive,
        DateTime toUtcExclusive,
        string timeZoneId,
        CancellationToken cancellationToken = default
    )
    {
        return await context.Database
            .SqlQuery<ClubDayCountRow>(
                $"""
                 SELECT date(timezone({timeZoneId}::text, v."EnterDate")) AS "Date", COUNT(*)::int AS "Count"
                 FROM "VisitLogs" AS v
                 WHERE v."EnterDate" >= {fromUtcInclusive} AND v."EnterDate" < {toUtcExclusive}
                 GROUP BY 1
                 ORDER BY 1
                 """
            )
            .ToListAsync(cancellationToken);
    }

    public async Task<VisitLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.VisitLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
    }

    public async Task<(IReadOnlyList<VisitLog> Items, int TotalCount)> GetPagedAsync(
        DateTime? enterFromUtc,
        DateTime? enterToUtc,
        Guid? clientId,
        bool? openOnly,
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default
    )
    {
        var query = context.VisitLogs.AsNoTracking().Include(v => v.Client).AsQueryable();

        if (enterFromUtc is { } from)
            query = query.Where(v => v.EnterDate >= from);

        if (enterToUtc is { } to)
            query = query.Where(v => v.EnterDate < to);

        if (clientId is { } cid)
            query = query.Where(v => v.ClientId == cid);

        if (openOnly == true)
            query = query.Where(v => v.LeaveDate == null);
        else if (openOnly == false)
            query = query.Where(v => v.LeaveDate != null);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var pattern = $"%{term}%";
            query = query.Where(v =>
                EF.Functions.ILike(v.Client.LastName, pattern)
                || EF.Functions.ILike(v.Client.FirstName, pattern)
                || EF.Functions.ILike(v.Client.MiddleName, pattern)
                || EF.Functions.ILike(v.Client.Phone, pattern)
                || EF.Functions.ILike(v.Client.Email, pattern)
            );
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(v => v.EnterDate)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}