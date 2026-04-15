using ZPassFit.Data.Models.Audit;

namespace ZPassFit.Data.Repositories.Audit;

public interface IAuditLogRepository
{
    Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> GetPagedAsync(
        DateTime? fromUtcInclusive,
        DateTime? toUtcExclusive,
        string? actionContains,
        string? entityTypeContains,
        int skip,
        int take,
        CancellationToken cancellationToken = default
    );

    Task<AuditLog?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
}
