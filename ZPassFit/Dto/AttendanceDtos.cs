namespace ZPassFit.Dto;

public record QrSessionResponse(Guid Token, DateTime ExpireDate);

public record VisitLogResponse(
    int Id,
    DateTime EnterDate,
    DateTime? LeaveDate,
    int MembershipId,
    Guid ClientId
);