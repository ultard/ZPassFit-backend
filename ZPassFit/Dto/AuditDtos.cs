namespace ZPassFit.Dto;

public record AuditLogResponse(
    long Id,
    DateTime OccurredAtUtc,
    string? UserId,
    string? UserEmail,
    string Action,
    string EntityType,
    string? EntityId,
    string? Details,
    string? IpAddress
);

public record PagedAuditLogsResponse(
    int Page,
    int PageSize,
    int TotalCount,
    IReadOnlyList<AuditLogResponse> Items
);
