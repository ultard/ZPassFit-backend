namespace ZPassFit.Dto;

public record QrSessionResponse(string Token, DateTime ExpireDate);

public record VisitLogResponse(
    int Id,
    DateTime EnterDate,
    DateTime? LeaveDate,
    int MembershipId,
    Guid ClientId
);