using CineVerse.Domain.Common;

namespace CineVerse.Domain.Entities;

/// <summary>Актор, що знімався у фільмах каталогу.</summary>
public class Actor : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }

    public ICollection<MovieActor> MovieActors { get; set; } = new List<MovieActor>();
}
