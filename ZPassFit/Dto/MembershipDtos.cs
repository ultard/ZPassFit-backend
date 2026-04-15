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

public record CreateMembershipPlanRequest(
    [property: MaxLength(32)] string Name,
    [property: MaxLength(128)] string Description,
    int[] Durations,
    [property: Range(0, int.MaxValue)] int Price
);

public record UpdateMembershipPlanRequest(
    [property: MaxLength(32)] string Name,
    [property: MaxLength(128)] string Description,
    int[] Durations,
    [property: Range(0, int.MaxValue)] int Price
);

/// <summary>
/// Выдать или продлить абонемент клиенту с дашборда (без привязки к платежу клиента).
/// </summary>
public record AdminSetMembershipRequest(
    Guid ClientId,
    int PlanId,
    [property: Range(1, 3650)] int DurationDays
);

/// <summary>
/// Частичное обновление абонемента (статус, тариф, даты).
/// </summary>
public record UpdateMembershipRequest(
    MembershipStatus? Status,
    int? PlanId,
    DateTime? ActivatedDate,
    DateTime? ExpireDate
);