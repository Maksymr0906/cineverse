namespace CineVerse.Application.Common.Interfaces;

/// <summary>Хешування та перевірка паролів (реалізація — BCrypt, п.2.2 ТЗ).</summary>
public interface IPasswordHasher
{
    string Hash(string plainPassword);
    bool Verify(string plainPassword, string passwordHash);
}
