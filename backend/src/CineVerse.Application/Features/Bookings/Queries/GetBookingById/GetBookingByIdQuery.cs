using CineVerse.Application.Features.Bookings.Common;
using MediatR;

namespace CineVerse.Application.Features.Bookings.Queries.GetBookingById;

/// <summary>UserId передається окремо (не з токена напряму в query), щоб хендлер міг перевірити
/// власника бронювання і не дати переглянути чужий booking (крім співробітників — перевіряється в контролері).</summary>
public record GetBookingByIdQuery(Guid BookingId, Guid RequestingUserId, bool IsStaff) : IRequest<BookingDto>;
