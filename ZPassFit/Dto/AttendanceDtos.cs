namespace ZPassFit.Dto;

public record QrSessionResponse(Guid Token, DateTime ExpireDate);

public record VisitLogResponse(
    int Id,
    DateTime EnterDate,
    DateTime? LeaveDate,
    int MembershipId,
    Guid ClientId
);

public record VisitLogListItemResponse(
    int Id,
    DateTime EnterDate,
    DateTime? LeaveDate,
    int MembershipId,
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