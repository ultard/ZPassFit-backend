using System.ComponentModel.DataAnnotations;
using ZPassFit.Data.Models.Memberships;

namespace ZPassFit.Dto;

public record MembershipPlanResponse(
    Guid Id,
    string Name,
    string Description,
    int[] Durations,
    int Price
);

public record MembershipResponse(
    Guid Id,
    Guid PlanId,
    MembershipStatus Status,
    DateTime ActivatedDate,
    DateTime ExpireDate
);

public record BuyMembershipRequest(
    Guid PlanId,
    int DurationDays,
    PaymentMethod Method
);

public record PaymentResponse(
    Guid Id,
    int Amount,
    PaymentMethod Method,
    PaymentStatus Status,
    DateTime CreateDate,
    DateTime? PaymentDate
);

public record PaymentMethodSettingResponse(
    PaymentMethod Method,
    string Code,
    string DisplayName,
    bool Enabled,
    string? Description
);

public record PaymentMethodsSettingsResponse(
    IReadOnlyList<PaymentMethodSettingResponse> Methods
);

public record CreateMembershipPlanRequest(
    [MaxLength(32)] string Name,
    [MaxLength(128)] string Description,
    int[] Durations,
    [Range(0, int.MaxValue)] int Price
);

public record UpdateMembershipPlanRequest(
    [MaxLength(32)] string Name,
    [MaxLength(128)] string Description,
    int[] Durations,
    [Range(0, int.MaxValue)] int Price
);

/// <summary>
/// Выдать или продлить абонемент клиенту с дашборда (без привязки к платежу клиента).
/// </summary>
public record AdminSetMembershipRequest(
    Guid ClientId,
    Guid PlanId,
    [Range(1, 3650)] int DurationDays
);

/// <summary>
/// Частичное обновление абонемента (статус, тариф, даты).
/// </summary>
public record UpdateMembershipRequest(
    MembershipStatus? Status,
    Guid? PlanId,
    DateTime? ActivatedDate,
    DateTime? ExpireDate
);

public record MembershipListItemResponse(
    Guid Id,
    Guid PlanId,
    string PlanName,
    Guid ClientId,
    string ClientLastName,
    string ClientFirstName,
    MembershipStatus Status,
    DateTime ActivatedDate,
    DateTime ExpireDate
);

public record PagedMembershipsResponse(
    int Page,
    int PageSize,
    int TotalCount,
    IReadOnlyList<MembershipListItemResponse> Items
);