using CineVerse.Domain.Common;
using CineVerse.Domain.Enums;

namespace CineVerse.Domain.Entities;

/// <summary>Кінозал у складі конкретного кінотеатру.</summary>
public class Hall : BaseEntity
{
    public Guid CinemaId { get; set; }
    public Cinema Cinema { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public HallType Type { get; set; } = HallType.STANDARD;
    public int Capacity { get; set; }

    public ICollection<Seat> Seats { get; set; } = new List<Seat>();
    public ICollection<Session> Sessions { get; set; } = new List<Session>();
}
