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

public record UpsertClientMeRequest(
    [property: MaxLength(100)] string LastName,
    [property: MaxLength(100)] string FirstName,
    [property: MaxLength(100)] string MiddleName,
    DateTime BirthDate,
    ClientGender Gender,
    [property: MaxLength(15)] string Phone,
    [property: MaxLength(255)] string Email,
    [property: MaxLength(100)] string? Notes
);

