using CineVerse.Application.Common.Interfaces;

namespace CineVerse.Infrastructure.Services;

public class SystemDateTime : IDateTime
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
