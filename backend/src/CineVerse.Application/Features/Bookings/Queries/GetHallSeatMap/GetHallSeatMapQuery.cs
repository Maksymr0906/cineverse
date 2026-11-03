using CineVerse.Application.Features.Bookings.Common;
using MediatR;

namespace CineVerse.Application.Features.Bookings.Queries.GetHallSeatMap;

/// <summary>
/// Актуальна схема залу для конкретного сеансу: кожне місце має статус FREE/LOCKED/SOLD
/// та розраховану ціну. Це те, що рендериться на сторінці вибору місць (п.2.1 ТЗ).
/// SOLD/LOCKED визначаються з БД (Booking.Status), а не з Redis: Redis використовується
/// лише як атомарний м'ютекс у момент створення бронювання (див. CreateBookingCommandHandler),
/// БД лишається єдиним джерелом істини для відображення статусів.
/// </summary>
public record GetHallSeatMapQuery(Guid SessionId) : IRequest<SeatMapDto>;
