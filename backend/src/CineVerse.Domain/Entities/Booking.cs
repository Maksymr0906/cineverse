using CineVerse.Domain.Common;
using CineVerse.Domain.Enums;

namespace CineVerse.Domain.Entities;

/// <summary>Бронювання квитків користувачем на конкретний сеанс.</summary>
public class Booking : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid SessionId { get; set; }
    public Session Session { get; set; } = null!;

    public BookingStatus Status { get; set; } = BookingStatus.PENDING;
    public decimal TotalPrice { get; set; }
    public DateTimeOffset ExpiresAt { get; set; } // CreatedAt + 10 хвилин

    // Промокод (п.2.1 ТЗ, сценарій "STUDENT20"): зберігаємо, ЯКИЙ саме код застосовано
    // і скільки знижки він дав — і для відображення в "Моїх бронюваннях", і для аудиту/підтримки.
    public Guid? PromotionId { get; set; }
    public Promotion? Promotion { get; set; }
    public decimal DiscountAmount { get; set; } = 0m;

    public ICollection<BookingSeat> BookingSeats { get; set; } = new List<BookingSeat>();
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    public Payment? Payment { get; set; }
}
