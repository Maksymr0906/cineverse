using CineVerse.Application.Common.Exceptions;
using CineVerse.Application.Common.Interfaces;
using CineVerse.Domain.Entities;
using CineVerse.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineVerse.Application.Features.Bookings.Commands.CancelBooking;

/// <summary>Скасування бронювання самим користувачем (поки не оплачене). Звільняє Redis-локи
/// та миттєво сповіщає інших глядачів сеансу, що місця знову вільні.</summary>
public class CancelBookingCommandHandler : IRequestHandler<CancelBookingCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ISeatLockService _seatLockService;
    private readonly ISeatStatusNotifier _seatStatusNotifier;

    public CancelBookingCommandHandler(IApplicationDbContext context, ISeatLockService seatLockService, ISeatStatusNotifier seatStatusNotifier)
    {
        _context = context;
        _seatLockService = seatLockService;
        _seatStatusNotifier = seatStatusNotifier;
    }

    public async Task Handle(CancelBookingCommand request, CancellationToken cancellationToken)
    {
        var booking = await _context.Bookings
            .Include(b => b.BookingSeats)
            .FirstOrDefaultAsync(b => b.Id == request.BookingId, cancellationToken)
            ?? throw new NotFoundException(nameof(Booking), request.BookingId);

        if (booking.UserId != request.RequestingUserId)
        {
            throw new UnauthorizedAccessException("Це бронювання належить іншому користувачу.");
        }

        if (booking.Status != BookingStatus.PENDING)
        {
            throw new ConflictException("Скасувати можна лише бронювання в статусі очікування оплати.");
        }

        booking.Status = BookingStatus.CANCELLED;
        await _context.SaveChangesAsync(cancellationToken);

        foreach (var bookingSeat in booking.BookingSeats)
        {
            await _seatLockService.ReleaseSeatLockAsync(booking.SessionId, bookingSeat.SeatId, cancellationToken);
        }

        await _seatStatusNotifier.NotifySeatsUpdatedAsync(
            booking.SessionId,
            booking.BookingSeats.Select(bs => new SeatStatusChange(bs.SeatId, "FREE")),
            cancellationToken);
    }
}
