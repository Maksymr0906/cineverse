using CineVerse.Application.Features.Bookings.Common;
using MediatR;

namespace CineVerse.Application.Features.Bookings.Queries.GetMyBookings;

/// <summary>Список бронювань поточного користувача для сторінки "Особистий кабінет" (п.2.1 ТЗ).</summary>
public record GetMyBookingsQuery(Guid UserId) : IRequest<IReadOnlyList<BookingDto>>;
