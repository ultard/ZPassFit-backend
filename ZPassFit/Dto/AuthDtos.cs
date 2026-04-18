namespace ZPassFit.Dto;

/// <param name="Email">Email адрес </param>
/// <param name="Password">Пароль </param>
/// <param name="LastName">Фамилия</param>
/// <param name="FirstName">Имя</param>
/// <param name="MiddleName">Отчество</param>
/// <param name="BirthDate">Дата рождения (UTC)</param>
/// <param name="Gender">Пол</param>
/// <param name="Phone">Телефон</param>
public record RegisterRequest(
    string Email,
    string Password,
    string LastName,
    string FirstName,
    string MiddleName,
    DateTime BirthDate,
    Data.Models.Clients.ClientGender Gender,
    string Phone
);

/// <param name="Status">Статус регистрации</param>
public record RegisterResponse(string Status);

/// <param name="Email">Email адрес</param>
/// <param name="Password">Пароль</param>
public record LoginRequest(string Email, string Password);

/// <param name="AccessToken">JWT для заголовка Authorization: Bearer</param>
/// <param name="RefreshToken">Токен для обновления пары</param>
/// <param name="AccessTokenExpiresAtUtc">Срок действия access-токена (UTC)</param>
public record AuthResponse(string AccessToken, string RefreshToken, DateTime AccessTokenExpiresAtUtc);

/// <param name="RefreshToken">Текущий refresh-токен</param>
public record RefreshRequest(string RefreshToken);

/// <param name="RefreshToken">Если указан — отзывается только он; иначе отзываются все refresh-токены пользователя</param>
public record LogoutRequest(string? RefreshToken);
