using Microsoft.EntityFrameworkCore;
using ZPassFit.Data.Models.Attendance;

namespace ZPassFit.Data.Repositories.Attendance;

public class QrSessionRepository(ApplicationDbContext context) : IQrSessionRepository
{
    public async Task<QrSession?> GetByTokenAsync(string token)
    {
        return await context.QrSessions
            .Include(q => q.Client)
            .FirstOrDefaultAsync(q => q.Token == token);
    }

    public async Task AddAsync(QrSession qrSession)
    {
        await context.QrSessions.AddAsync(qrSession);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(QrSession qrSession)
    {
        context.QrSessions.Update(qrSession);
        await context.SaveChangesAsync();
    }

    public async Task DeleteByTokenAsync(string token)
    {
        var session = await GetByTokenAsync(token);
        if (session != null)
        {
            context.QrSessions.Remove(session);
            await context.SaveChangesAsync();
        }
    }
}