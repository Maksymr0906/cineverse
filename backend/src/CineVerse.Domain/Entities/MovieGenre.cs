namespace CineVerse.Domain.Entities;

/// <summary>Зв'язуюча сутність "фільм-жанр" (composite key: MovieId + GenreId).</summary>
public class MovieGenre
{
    public Guid MovieId { get; set; }
    public Movie Movie { get; set; } = null!;

    public Guid GenreId { get; set; }
    public Genre Genre { get; set; } = null!;
}
