namespace CineVerse.Application.Common.Exceptions;

/// <summary>Кидається, коли запитувану сутність не знайдено. Мапиться на HTTP 404.</summary>
public class NotFoundException : Exception
{
    public NotFoundException(string entityName, object key)
        : base($"Сутність \"{entityName}\" з ключем ({key}) не знайдено.") { }

    public NotFoundException(string message) : base(message) { }
}
