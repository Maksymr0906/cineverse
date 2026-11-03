namespace CineVerse.Application.Common.Exceptions;

/// <summary>Кидається при конфлікті бізнес-стану (напр. email вже зайнято, місце вже заброньоване). Мапиться на HTTP 409.</summary>
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}
