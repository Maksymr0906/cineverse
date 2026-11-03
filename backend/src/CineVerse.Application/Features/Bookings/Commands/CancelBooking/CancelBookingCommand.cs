using MediatR;

namespace CineVerse.Application.Features.Bookings.Commands.CancelBooking;

public record CancelBookingCommand(Guid BookingId, Guid RequestingUserId) : IRequest;
