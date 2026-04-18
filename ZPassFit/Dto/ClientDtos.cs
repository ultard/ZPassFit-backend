using System.ComponentModel.DataAnnotations;
using ZPassFit.Data.Models.Clients;

namespace ZPassFit.Dto;

public record ClientResponse(
    Guid Id,
    string LastName,
    string FirstName,
    string MiddleName,
    DateTime BirthDate,
    ClientGender Gender,
    string Phone,
    string Email,
    DateTime RegistrationDate,
    ClientStatus Status,
    int Bonuses,
    string? Notes
);

/// <summary>
/// Краткая карточка клиента для списков в дашборде.
/// </summary>
public record ClientListItemResponse(
    Guid Id,
    string LastName,
    string FirstName,
    string MiddleName,
    string Phone,
    string Email,
    ClientStatus Status,
    DateTime RegistrationDate
);

public record PagedClientsResponse(
    int Page,
    int PageSize,
    int TotalCount,
    IReadOnlyList<ClientListItemResponse> Items
);

/// <summary>
/// Активный уровень лояльности текущего клиента.
/// </summary>
public record MyClientLevelResponse(
    int ClientLevelId,
    DateTime ReceiveDate,
    LevelResponse Level
);