namespace CineVerse.Application.Common.Interfaces;

/// <summary>Надає доступ до даних поточного автентифікованого користувача (з JWT claims).</summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    string? Role { get; }
}
