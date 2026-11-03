using CineVerse.Domain.Common;
using CineVerse.Domain.Enums;

namespace CineVerse.Domain.Entities;

/// <summary>Електронний квиток з унікальним кодом та QR-кодом для верифікації на вході.</summary>
public class Ticket : BaseEntity
{
    public Guid BookingId { get; set; }
    public Booking Booking { get; set; } = null!;

    public Guid SessionId { get; set; }
    public Session Session { get; set; } = null!;

    public Guid SeatId { get; set; }
    public Seat Seat { get; set; } = null!;

    public string TicketCode { get; set; } = string.Empty; // унікальний код (GUID/JWT)
    public string QrCode { get; set; } = string.Empty;     // base64 зображення або посилання
    public TicketStatus Status { get; set; } = TicketStatus.ACTIVE;

    // Аудит перевірки на вході в зал (Етап 7, п.2.2 ТЗ: кабінет контролера)
    public DateTimeOffset? ValidatedAt { get; set; }
    public Guid? ValidatedByUserId { get; set; }
}
