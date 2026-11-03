using CineVerse.Domain.Common;

namespace CineVerse.Domain.Entities;

/// <summary>Конкретне місце у складі бронювання із зафіксованою на момент бронювання ціною.</summary>
public class BookingSeat : BaseEntity
{
    public Guid BookingId { get; set; }
    public Booking Booking { get; set; } = null!;

    public Guid SeatId { get; set; }
    public Seat Seat { get; set; } = null!;

    public decimal Price { get; set; }
}
