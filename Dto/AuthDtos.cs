namespace ZPassFit.Dto;

/// <param name="Email">Email адрес </param>
/// <param name="Password">Пароль</param>
public record RegisterRequest(string Email, string Password);

/// <param name="Email">Email адрес</param>
/// <param name="Password">Пароль</param>
public record LoginRequest(string Email, string Password);
