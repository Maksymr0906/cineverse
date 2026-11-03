using CineVerse.Domain.Common;
using CineVerse.Domain.Enums;

namespace CineVerse.Domain.Entities;

/// <summary>Фільм у каталозі кінотеатру.</summary>
public class Movie : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Duration { get; set; } // хвилини
    public DateOnly ReleaseDate { get; set; }
    public string AgeRating { get; set; } = string.Empty;
    public string PosterUrl { get; set; } = string.Empty;
    public string TrailerUrl { get; set; } = string.Empty;
    public MovieStatus Status { get; set; } = MovieStatus.COMING_SOON;

    public ICollection<MovieGenre> MovieGenres { get; set; } = new List<MovieGenre>();
    public ICollection<MovieActor> MovieActors { get; set; } = new List<MovieActor>();
    public ICollection<Session> Sessions { get; set; } = new List<Session>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}
