using CineVerse.Domain.Common;
using CineVerse.Domain.Enums;

namespace CineVerse.Domain.Entities;

/// <summary>Користувач системи: клієнт або співробітник мережі кінотеатрів.</summary>
public class User : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.CUSTOMER;
    public bool IsEmailConfirmed { get; set; } = false;
    public string? EmailConfirmationToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTimeOffset? RefreshTokenExpiresAt { get; set; }

    // Відновлення пароля (п. "Бажано, якщо лишиться час" аудиту) — той самий патерн,
    // що й EmailConfirmationToken: одноразовий токен з обмеженим часом дії.
    public string? PasswordResetToken { get; set; }
    public DateTimeOffset? PasswordResetTokenExpiresAt { get; set; }

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}
