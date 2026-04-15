using Microsoft.EntityFrameworkCore;
using ZPassFit.Data.Models.Memberships;

namespace ZPassFit.Data.Repositories.Memberships;

public class PaymentRepository(ApplicationDbContext context) : IPaymentRepository
{
    public async Task<Payment?> GetByIdAsync(int id)
    {
        return await context.Payments
            .Include(p => p.Client)
            .Include(p => p.Employee)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<IEnumerable<Payment>> GetByClientIdAsync(Guid clientId)
    {
        return await context.Payments
            .Where(p => p.ClientId == clientId)
            .OrderByDescending(p => p.CreateDate)
            .ToListAsync();
    }

    public async Task AddAsync(Payment payment)
    {
        await context.Payments.AddAsync(payment);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Payment payment)
    {
        context.Payments.Update(payment);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var payment = await GetByIdAsync(id);
        if (payment == null) return;

        context.Payments.Remove(payment);
        await context.SaveChangesAsync();
    }

    public async Task<(int Count, long TotalAmount)> GetCompletedPaymentsSummaryBetweenAsync(
        DateTime fromUtcInclusive,
        DateTime toUtcExclusive
    )
    {
        var query = context.Payments.Where(p =>
            p.Status == PaymentStatus.Completed
            && (p.PaymentDate ?? p.CreateDate) >= fromUtcInclusive
            && (p.PaymentDate ?? p.CreateDate) < toUtcExclusive
        );

        var count = await query.CountAsync();
        var total = await query.SumAsync(p => (long)p.Amount);
        return (count, total);
    }
}