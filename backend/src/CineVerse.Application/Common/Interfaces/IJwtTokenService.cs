namespace CineVerse.Application.Common.Interfaces;

public record TokenPair(string AccessToken, string RefreshToken, DateTimeOffset RefreshTokenExpiresAt);

/// <summary>
/// Видача пари токенів JWT Access (15 хв) + Refresh (7 днів, зберігається у HttpOnly Cookie),
/// відповідно до п.2.2 ТЗ.
/// </summary>
public interface IJwtTokenService
{
    TokenPair GenerateTokenPair(Guid userId, string email, string role);

    /// <summary>Криптографічно стійкий випадковий refresh-токен (не JWT).</summary>
    string GenerateRefreshTokenValue();
}
