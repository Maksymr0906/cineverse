namespace CineVerse.Application.Features.Auth.Common;

/// <summary>Результат успішного логіну/оновлення токена, що повертається клієнту.
/// AccessToken іде в тілі відповіді, RefreshToken окремо встановлюється у HttpOnly Cookie на WebApi-рівні.</summary>
public record AuthResultDto(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    Guid UserId,
    string FirstName,
    string LastName,
    string Email,
    string Role
);
