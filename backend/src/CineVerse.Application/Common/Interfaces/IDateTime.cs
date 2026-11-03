namespace CineVerse.Application.Common.Interfaces;

/// <summary>Абстракція над поточним часом — для детермінованого юніт-тестування (TTL, таймери).</summary>
public interface IDateTime
{
    DateTimeOffset UtcNow { get; }
}
