using CineVerse.Application.Features.Bookings.Common;
using MediatR;

namespace CineVerse.Application.Features.Bookings.Commands.CreateBooking;

/// <summary>
/// Ініціює бронювання: атомарно блокує обрані місця в Redis, створює Booking(PENDING)
/// з таймером на 10 хвилин (п.2.2 ТЗ). Оплата — окремий крок (Етап 6), після якого
/// статус переходить у CONFIRMED, а місця — фізично в SOLD.
///
/// PromoCode (опційно) — промокод застосовується до ЗАГАЛЬНОЇ суми бронювання (не до
/// ціни окремого місця), реалізує сценарій використання з ТЗ ("STUDENT20" — знижка 20%).
/// </summary>
public record CreateBookingCommand(Guid UserId, Guid SessionId, IReadOnlyList<Guid> SeatIds, string? PromoCode = null) : IRequest<BookingDto>;
