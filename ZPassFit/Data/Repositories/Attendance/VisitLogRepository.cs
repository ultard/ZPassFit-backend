using Microsoft.EntityFrameworkCore;
using ZPassFit.Data.Models.Attendance;

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
}