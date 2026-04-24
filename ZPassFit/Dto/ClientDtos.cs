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
/// <param name="NextLevel">Следующий уровень в цепочке; null, если достигнут максимальный.</param>
/// <param name="RemainingDaysToNextLevel">
/// Сколько уникальных дней с посещениями не хватает до порога <see cref="LevelResponse.ActivateDays"/> следующего уровня
/// (счёт с даты регистрации клиента). null, если следующего уровня нет.
/// </param>
public record MyClientLevelResponse(
    Guid ClientLevelId,
    DateTime ReceiveDate,
    LevelResponse Level,
    LevelResponse? NextLevel,
    int? RemainingDaysToNextLevel
);