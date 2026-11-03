using CineVerse.Application.Common.Exceptions;
using CineVerse.Application.Common.Interfaces;
using CineVerse.Application.Features.Tickets.Services;
using CineVerse.Domain.Entities;
using CineVerse.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineVerse.Application.Features.Payments.Commands.ProcessPaymentWebhook;

public class ProcessPaymentWebhookCommandHandler : IRequestHandler<ProcessPaymentWebhookCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IPaymentGatewayService _paymentGateway;
    private readonly ISeatLockService _seatLockService;
    private readonly ISeatStatusNotifier _seatStatusNotifier;
    private readonly TicketIssuanceService _ticketIssuanceService;

    public ProcessPaymentWebhookCommandHandler(
        IApplicationDbContext context,
        IPaymentGatewayService paymentGateway,
        ISeatLockService seatLockService,
        ISeatStatusNotifier seatStatusNotifier,
        TicketIssuanceService ticketIssuanceService)
    {
        _context = context;
        _paymentGateway = paymentGateway;
        _seatLockService = seatLockService;
        _seatStatusNotifier = seatStatusNotifier;
        _ticketIssuanceService = ticketIssuanceService;
    }

    public async Task Handle(ProcessPaymentWebhookCommand request, CancellationToken cancellationToken)
    {
        if (!_paymentGateway.VerifyWebhookSignature(request.RawPayload, request.Signature))
        {
            // Навмисно НЕ деталізуємо причину в повідомленні назовні — щоб не підказувати
            // зловмиснику, як підробити валідний підпис.
            throw new AuthenticationException("Недійсний підпис Webhook.");
        }

        var payment = await _context.Payments
            .Include(p => p.Booking).ThenInclude(b => b.BookingSeats)
            .Include(p => p.Booking).ThenInclude(b => b.User)
            .FirstOrDefaultAsync(p => p.TransactionId == request.TransactionId, cancellationToken)
            ?? throw new NotFoundException("Платіж із таким TransactionId не знайдено.");

        // Ідемпотентність: платіжні шлюзи можуть надіслати той самий Webhook кілька разів
        // (retry на випадок нестабільної мережі) — повторна обробка вже завершеного платежу є no-op.
        if (payment.Status != PaymentStatus.PENDING)
        {
            return;
        }

        var booking = payment.Booking;

        if (request.IsSuccess)
        {
            payment.Status = PaymentStatus.SUCCESS;
            booking.Status = BookingStatus.CONFIRMED;

            // Квитки додаються в контекст РАЗОМ зі зміною статусів і зберігаються ОДНИМ
            // SaveChangesAsync нижче — якщо створення квитків з якоїсь причини провалиться,
            // відкотиться все, включно з Payment.Status/Booking.Status (див. коментар у TicketIssuanceService).
            var tickets = _ticketIssuanceService.PrepareTickets(booking);

            await _context.SaveChangesAsync(cancellationToken);

            // Місце вже незворотно продане в БД — Redis-лок як тимчасовий м'ютекс більше не потрібен.
            foreach (var bookingSeat in booking.BookingSeats)
            {
                await _seatLockService.ReleaseSeatLockAsync(booking.SessionId, bookingSeat.SeatId, cancellationToken);
            }

            await _seatStatusNotifier.NotifySeatsUpdatedAsync(
                booking.SessionId,
                booking.BookingSeats.Select(bs => new SeatStatusChange(bs.SeatId, "SOLD")),
                cancellationToken);

            // Поза транзакцією й гарантовано без винятку — див. коментар у SendTicketEmailSafeAsync.
            await _ticketIssuanceService.SendTicketEmailSafeAsync(booking.User.Email, tickets.Select(t => t.Id), cancellationToken);
        }
        else
        {
            payment.Status = PaymentStatus.FAILED;
            booking.Status = BookingStatus.CANCELLED;
            await _context.SaveChangesAsync(cancellationToken);

            // Невдала оплата — одразу звільняємо місця, а не чекаємо на ExpiredBookingWorker,
            // щоб не тримати їх заблокованими даремно до кінця 10-хвилинного вікна.
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
}
