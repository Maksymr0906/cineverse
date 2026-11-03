using CineVerse.Application.Common.Exceptions;
using CineVerse.Application.Common.Interfaces;
using CineVerse.Application.Features.Tickets.Common;
using CineVerse.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineVerse.Application.Features.Tickets.Queries.GetTicketsByBooking;

public class GetTicketsByBookingQueryHandler : IRequestHandler<GetTicketsByBookingQuery, IReadOnlyList<TicketDto>>
{
    private readonly IApplicationDbContext _context;

    public GetTicketsByBookingQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<TicketDto>> Handle(GetTicketsByBookingQuery request, CancellationToken cancellationToken)
    {
        var booking = await _context.Bookings
            .FirstOrDefaultAsync(b => b.Id == request.BookingId, cancellationToken)
            ?? throw new NotFoundException(nameof(Booking), request.BookingId);

        if (booking.UserId != request.RequestingUserId && !request.IsStaff)
        {
            throw new UnauthorizedAccessException("Це бронювання належить іншому користувачу.");
        }

        return await _context.Tickets
            .Where(t => t.BookingId == request.BookingId)
            .Include(t => t.Seat)
            .OrderBy(t => t.Seat.RowNumber).ThenBy(t => t.Seat.SeatNumber)
            .Select(t => new TicketDto(t.Id, t.Seat.RowNumber, t.Seat.SeatNumber, t.TicketCode, t.QrCode, t.Status.ToString()))
            .ToListAsync(cancellationToken);
    }
}
