namespace CineVerse.Application.Common.Exceptions;

/// <summary>Кидається при невдалій автентифікації (невірний email/пароль, протермінований токен). Мапиться на HTTP 401.</summary>
public class AuthenticationException : Exception
{
    public AuthenticationException(string message) : base(message) { }
}
