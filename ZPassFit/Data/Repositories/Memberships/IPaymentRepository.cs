using ZPassFit.Data.Models.Memberships;
using ZPassFit.Data.Repositories;

namespace ZPassFit.Data.Repositories.Memberships;

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(int id);
    Task<IEnumerable<Payment>> GetByClientIdAsync(Guid clientId);
    Task AddAsync(Payment payment);
    Task UpdateAsync(Payment payment);
    Task DeleteAsync(int id);

    Task<(int Count, long TotalAmount)> GetCompletedPaymentsSummaryBetweenAsync(
        DateTime fromUtcInclusive,
        DateTime toUtcExclusive
    );

    Task<IReadOnlyList<ClubDayRevenueRow>> GetCompletedPaymentAmountsByClubDayAsync(
        DateTime fromUtcInclusive,
        DateTime toUtcExclusive,
        string timeZoneId,
        CancellationToken cancellationToken = default
    );
}