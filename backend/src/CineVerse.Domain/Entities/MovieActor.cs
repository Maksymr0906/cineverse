namespace CineVerse.Domain.Entities;

/// <summary>Зв'язуюча сутність "фільм-актор" з іменем персонажа.</summary>
public class MovieActor
{
    public Guid MovieId { get; set; }
    public Movie Movie { get; set; } = null!;

    public Guid ActorId { get; set; }
    public Actor Actor { get; set; } = null!;

    public string CharacterName { get; set; } = string.Empty;
}
