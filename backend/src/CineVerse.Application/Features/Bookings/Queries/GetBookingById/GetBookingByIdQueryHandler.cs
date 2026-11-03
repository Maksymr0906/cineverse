using CineVerse.Application.Common.Exceptions;
using CineVerse.Application.Common.Interfaces;
using CineVerse.Application.Features.Bookings.Common;
using CineVerse.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineVerse.Application.Features.Bookings.Queries.GetBookingById;

public class GetBookingByIdQueryHandler : IRequestHandler<GetBookingByIdQuery, BookingDto>
{
    private readonly IApplicationDbContext _context;

    public GetBookingByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<BookingDto> Handle(GetBookingByIdQuery request, CancellationToken cancellationToken)
    {
        var booking = await _context.Bookings
            .Include(b => b.BookingSeats).ThenInclude(bs => bs.Seat)
            .Include(b => b.Session).ThenInclude(s => s.Movie)
            .Include(b => b.Session).ThenInclude(s => s.Hall).ThenInclude(h => h.Cinema)
            .Include(b => b.Promotion)
            .FirstOrDefaultAsync(b => b.Id == request.BookingId, cancellationToken)
            ?? throw new NotFoundException(nameof(Booking), request.BookingId);

        if (booking.UserId != request.RequestingUserId && !request.IsStaff)
        {
            throw new UnauthorizedAccessException("Це бронювання належить іншому користувачу.");
        }

        return new BookingDto(
            booking.Id,
            booking.SessionId,
            booking.Status.ToString(),
            booking.TotalPrice,
            booking.ExpiresAt,
            booking.BookingSeats.Select(bs => new BookingSeatResultDto(bs.SeatId, bs.Seat.RowNumber, bs.Seat.SeatNumber, bs.Price)).ToList(),
            booking.Session.Movie.Title,
            booking.Session.Movie.PosterUrl,
            booking.Session.Hall.Name,
            booking.Session.Hall.Cinema.Name,
            booking.Session.StartTime,
            booking.Promotion?.Code,
            booking.DiscountAmount
        );
    }
}
