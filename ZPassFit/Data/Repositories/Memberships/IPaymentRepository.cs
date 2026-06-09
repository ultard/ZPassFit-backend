using ZPassFit.Data.Models.Memberships;

namespace ZPassFit.Data.Repositories.Memberships;

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(Guid id);

    Task<Payment?> GetByYooKassaPaymentIdAsync(string yooKassaPaymentId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Payment>> GetByClientIdAsync(Guid clientId);
    Task AddAsync(Payment payment);
    Task UpdateAsync(Payment payment);
    Task DeleteAsync(Guid id);

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

    Task<IReadOnlyList<ClubDayRevenueRow>> GetCompletedPaymentAmountsByClubDayForClientAsync(
        Guid clientId,
        DateTime fromUtcInclusive,
        DateTime toUtcExclusive,
        string timeZoneId,
        CancellationToken cancellationToken = default
    );
}