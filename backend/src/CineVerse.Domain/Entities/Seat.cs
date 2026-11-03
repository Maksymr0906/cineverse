using CineVerse.Domain.Common;
using CineVerse.Domain.Enums;

namespace CineVerse.Domain.Entities;

/// <summary>Фізичне місце (крісло) у залі. Статус місця на конкретний сеанс
/// обчислюється динамічно (Redis lock + таблиця Ticket), тут зберігається лише конфігурація крісла.</summary>
public class Seat : BaseEntity
{
    public Guid HallId { get; set; }
    public Hall Hall { get; set; } = null!;

    public int RowNumber { get; set; }
    public int SeatNumber { get; set; }
    public SeatType Type { get; set; } = SeatType.STANDARD;

    public ICollection<BookingSeat> BookingSeats { get; set; } = new List<BookingSeat>();
}
