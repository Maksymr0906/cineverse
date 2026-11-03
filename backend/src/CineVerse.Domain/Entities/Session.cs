using CineVerse.Domain.Common;

namespace CineVerse.Domain.Entities;

/// <summary>Сеанс — показ конкретного фільму у конкретному залі у визначений час.</summary>
public class Session : BaseEntity
{
    public Guid MovieId { get; set; }
    public Movie Movie { get; set; } = null!;

    public Guid HallId { get; set; }
    public Hall Hall { get; set; } = null!;

    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; } // StartTime + Movie.Duration + 20 хв технічної перерви
    public string Format { get; set; } = "2D"; // 2D, 3D, IMAX, 4DX
    public decimal BasePrice { get; set; }

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
