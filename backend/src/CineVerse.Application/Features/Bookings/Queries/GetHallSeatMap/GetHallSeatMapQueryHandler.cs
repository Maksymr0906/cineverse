using CineVerse.Application.Common.Exceptions;
using CineVerse.Application.Common.Interfaces;
using CineVerse.Application.Features.Bookings.Common;
using CineVerse.Domain.Entities;
using CineVerse.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineVerse.Application.Features.Bookings.Queries.GetHallSeatMap;

public class GetHallSeatMapQueryHandler : IRequestHandler<GetHallSeatMapQuery, SeatMapDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTime _dateTime;

    public GetHallSeatMapQueryHandler(IApplicationDbContext context, IDateTime dateTime)
    {
        _context = context;
        _dateTime = dateTime;
    }

    public async Task<SeatMapDto> Handle(GetHallSeatMapQuery request, CancellationToken cancellationToken)
    {
        var session = await _context.Sessions
            .Include(s => s.Hall).ThenInclude(h => h.Seats)
            .FirstOrDefaultAsync(s => s.Id == request.SessionId, cancellationToken)
            ?? throw new NotFoundException(nameof(Session), request.SessionId);

        var now = _dateTime.UtcNow;

        // Місця, зайняті на цей сеанс: SOLD — підтверджене бронювання; LOCKED — активне (ще не протерміноване) PENDING.
        var occupiedSeats = await _context.BookingSeats
            .Include(bs => bs.Booking)
            .Where(bs => bs.Booking.SessionId == request.SessionId &&
                        (bs.Booking.Status == BookingStatus.CONFIRMED ||
                        (bs.Booking.Status == BookingStatus.PENDING && bs.Booking.ExpiresAt > now)))
            .Select(bs => new { bs.SeatId, bs.Booking.Status })
            .ToDictionaryAsync(x => x.SeatId, x => x.Status, cancellationToken);

        var seats = session.Hall.Seats
            .OrderBy(s => s.RowNumber).ThenBy(s => s.SeatNumber)
            .Select(seat =>
            {
                var status = occupiedSeats.TryGetValue(seat.Id, out var bookingStatus)
                    ? (bookingStatus == BookingStatus.CONFIRMED ? "SOLD" : "LOCKED")
                    : "FREE";

                var price = SeatPricingCalculator.CalculatePrice(session.BasePrice, seat.Type);

                return new SeatMapSeatDto(seat.Id, seat.RowNumber, seat.SeatNumber, seat.Type.ToString(), status, price);
            })
            .ToList();

        return new SeatMapDto(session.Id, session.HallId, session.Hall.Name, seats);
    }
}
