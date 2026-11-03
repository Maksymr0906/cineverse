using CineVerse.Domain.Common;

namespace CineVerse.Domain.Entities;

/// <summary>Жанр фільму (напр. Драма, Бойовик, Фантастика).</summary>
public class Genre : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public ICollection<MovieGenre> MovieGenres { get; set; } = new List<MovieGenre>();
}
