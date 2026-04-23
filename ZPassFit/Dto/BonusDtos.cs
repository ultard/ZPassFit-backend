using ZPassFit.Data.Models.Clients;

namespace ZPassFit.Dto;

public record BonusTransactionListItemResponse(
    Guid Id,
    BonusTransactionType Type,
    DateTime CreateDate,
    DateTime? ExpireDate,
    Guid ClientId,
    string ClientLastName,
    string ClientFirstName
);

public record PagedBonusTransactionsResponse(
    int Page,
    int PageSize,
    int TotalCount,
    IReadOnlyList<BonusTransactionListItemResponse> Items
);
