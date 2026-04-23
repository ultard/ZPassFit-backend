namespace ZPassFit.Dto;

public record QrSessionResponse(Guid Token, DateTime ExpireDate);

public record VisitLogResponse(
    Guid Id,
    DateTime EnterDate,
    DateTime? LeaveDate,
    Guid MembershipId,
    Guid ClientId
);

public record VisitLogListItemResponse(
    Guid Id,
    DateTime EnterDate,
    DateTime? LeaveDate,
    Guid MembershipId,
    Guid ClientId,
    string ClientLastName,
    string ClientFirstName
);

public record PagedVisitLogsResponse(
    int Page,
    int PageSize,
    int TotalCount,
    IReadOnlyList<VisitLogListItemResponse> Items
);