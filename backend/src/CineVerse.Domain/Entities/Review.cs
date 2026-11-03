using CineVerse.Domain.Common;

namespace CineVerse.Domain.Entities;

/// <summary>Відгук користувача на фільм. Верифікований — лише після сканування квитка контролером.</summary>
public class Review : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid MovieId { get; set; }
    public Movie Movie { get; set; } = null!;

    public int Rating { get; set; } // 1-5
    public string Comment { get; set; } = string.Empty;
    public bool IsVerified { get; set; } = false;
}
