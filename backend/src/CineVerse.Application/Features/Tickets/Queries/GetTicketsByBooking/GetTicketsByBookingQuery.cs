using CineVerse.Application.Features.Tickets.Common;
using MediatR;

namespace CineVerse.Application.Features.Tickets.Queries.GetTicketsByBooking;

public record GetTicketsByBookingQuery(Guid BookingId, Guid RequestingUserId, bool IsStaff) : IRequest<IReadOnlyList<TicketDto>>;
