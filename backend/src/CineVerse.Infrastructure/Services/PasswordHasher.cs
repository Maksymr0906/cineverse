using CineVerse.Application.Common.Interfaces;

namespace CineVerse.Infrastructure.Services;

/// <summary>Реалізація хешування паролів через BCrypt.Net-Next (work factor 11), п.2.2 ТЗ.</summary>
public class PasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 11;

    public string Hash(string plainPassword) => BCrypt.Net.BCrypt.HashPassword(plainPassword, WorkFactor);

    public bool Verify(string plainPassword, string passwordHash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(plainPassword, passwordHash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // Хеш пошкоджено або має несумісний формат — трактуємо як невдалу автентифікацію, а не 500-помилку.
            return false;
        }
    }
}
