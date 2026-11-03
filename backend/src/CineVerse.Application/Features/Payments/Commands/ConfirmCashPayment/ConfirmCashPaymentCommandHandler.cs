using CineVerse.Application.Common.Exceptions;
using CineVerse.Application.Common.Interfaces;
using CineVerse.Application.Features.Tickets.Services;
using CineVerse.Domain.Entities;
using CineVerse.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineVerse.Application.Features.Payments.Commands.ConfirmCashPayment;

public class ConfirmCashPaymentCommandHandler : IRequestHandler<ConfirmCashPaymentCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ISeatLockService _seatLockService;
    private readonly ISeatStatusNotifier _seatStatusNotifier;
    private readonly TicketIssuanceService _ticketIssuanceService;
    private readonly IDateTime _dateTime;

    public ConfirmCashPaymentCommandHandler(
        IApplicationDbContext context,
        ISeatLockService seatLockService,
        ISeatStatusNotifier seatStatusNotifier,
        TicketIssuanceService ticketIssuanceService,
        IDateTime dateTime)
    {
        _context = context;
        _seatLockService = seatLockService;
        _seatStatusNotifier = seatStatusNotifier;
        _ticketIssuanceService = ticketIssuanceService;
        _dateTime = dateTime;
    }

    public async Task Handle(ConfirmCashPaymentCommand request, CancellationToken cancellationToken)
    {
        var booking = await _context.Bookings
            .Include(b => b.BookingSeats)
            .Include(b => b.User)
            .FirstOrDefaultAsync(b => b.Id == request.BookingId, cancellationToken)
            ?? throw new NotFoundException(nameof(Booking), request.BookingId);

        if (booking.Status != BookingStatus.PENDING)
        {
            throw new ConflictException($"Неможливо підтвердити оплату: бронювання має статус {booking.Status}.");
        }

        if (booking.ExpiresAt <= _dateTime.UtcNow)
        {
            throw new ConflictException("Час бронювання вичерпано. Створіть нове бронювання.");
        }

        var payment = new Payment
        {
            BookingId = booking.Id,
            Amount = booking.TotalPrice,
            Method = PaymentMethod.CASH,
            Status = PaymentStatus.SUCCESS,
            TransactionId = $"CASH-{request.CashierUserId:N}-{_dateTime.UtcNow:yyyyMMddHHmmss}"
        };

        _context.Payments.Add(payment);
        booking.Status = BookingStatus.CONFIRMED;

        // Той самий принцип атомарності, що й у ProcessPaymentWebhookCommandHandler:
        // квитки зберігаються ОДНИМ SaveChangesAsync разом зі зміною статусів.
        var tickets = _ticketIssuanceService.PrepareTickets(booking);
        await _context.SaveChangesAsync(cancellationToken);

        foreach (var bookingSeat in booking.BookingSeats)
        {
            await _seatLockService.ReleaseSeatLockAsync(booking.SessionId, bookingSeat.SeatId, cancellationToken);
        }

        await _seatStatusNotifier.NotifySeatsUpdatedAsync(
            booking.SessionId,
            booking.BookingSeats.Select(bs => new SeatStatusChange(bs.SeatId, "SOLD")),
            cancellationToken);

        await _ticketIssuanceService.SendTicketEmailSafeAsync(booking.User.Email, tickets.Select(t => t.Id), cancellationToken);
    }
}
