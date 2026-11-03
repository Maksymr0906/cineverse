using CineVerse.Application.Common.Interfaces;
using CineVerse.Application.Features.Bookings.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineVerse.Application.Features.Bookings.Queries.GetMyBookings;

public class GetMyBookingsQueryHandler : IRequestHandler<GetMyBookingsQuery, IReadOnlyList<BookingDto>>
{
    private readonly IApplicationDbContext _context;

    public GetMyBookingsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<BookingDto>> Handle(GetMyBookingsQuery request, CancellationToken cancellationToken)
    {
        var bookings = await _context.Bookings
            .Where(b => b.UserId == request.UserId)
            .Include(b => b.BookingSeats).ThenInclude(bs => bs.Seat)
            .Include(b => b.Session).ThenInclude(s => s.Movie)
            .Include(b => b.Session).ThenInclude(s => s.Hall).ThenInclude(h => h.Cinema)
            .Include(b => b.Promotion)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(cancellationToken);

        return bookings.Select(b => new BookingDto(
            b.Id, b.SessionId, b.Status.ToString(), b.TotalPrice, b.ExpiresAt,
            b.BookingSeats.Select(bs => new BookingSeatResultDto(bs.SeatId, bs.Seat.RowNumber, bs.Seat.SeatNumber, bs.Price)).ToList(),
            b.Session.Movie.Title, b.Session.Movie.PosterUrl, b.Session.Hall.Name, b.Session.Hall.Cinema.Name, b.Session.StartTime,
            b.Promotion?.Code, b.DiscountAmount
        )).ToList();
    }
}
