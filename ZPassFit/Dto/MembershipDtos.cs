using System.ComponentModel.DataAnnotations;
using ZPassFit.Data.Models.Memberships;

namespace ZPassFit.Dto;

public record MembershipPlanResponse(
    int Id,
    string Name,
    string Description,
    int[] Durations,
    int Price
);

public record MembershipResponse(
    int Id,
    int PlanId,
    MembershipStatus Status,
    DateTime ActivatedDate,
    DateTime ExpireDate
);

public record BuyMembershipRequest(
    int PlanId,
    int DurationDays,
    PaymentMethod Method
);

public record PaymentResponse(
    int Id,
    int Amount,
    PaymentMethod Method,
    PaymentStatus Status,
    DateTime CreateDate,
    DateTime? PaymentDate
);

