using CineVerse.Application.Common.Exceptions;
using CineVerse.Application.Common.Interfaces;
using CineVerse.Application.Features.Payments.Common;
using CineVerse.Domain.Entities;
using CineVerse.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineVerse.Application.Features.Payments.Commands.InitiatePayment;

public class InitiatePaymentCommandHandler : IRequestHandler<InitiatePaymentCommand, PaymentInitiationResultDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IPaymentGatewayService _paymentGateway;
    private readonly IDateTime _dateTime;

    public InitiatePaymentCommandHandler(IApplicationDbContext context, IPaymentGatewayService paymentGateway, IDateTime dateTime)
    {
        _context = context;
        _paymentGateway = paymentGateway;
        _dateTime = dateTime;
    }

    public async Task<PaymentInitiationResultDto> Handle(InitiatePaymentCommand request, CancellationToken cancellationToken)
    {
        var booking = await _context.Bookings
            .Include(b => b.Payment)
            .FirstOrDefaultAsync(b => b.Id == request.BookingId, cancellationToken)
            ?? throw new NotFoundException(nameof(Booking), request.BookingId);

        if (booking.UserId != request.UserId)
        {
            throw new UnauthorizedAccessException("Це бронювання належить іншому користувачу.");
        }

        if (booking.Status != BookingStatus.PENDING)
        {
            throw new ConflictException($"Оплата недоступна: бронювання має статус {booking.Status}.");
        }

        if (booking.ExpiresAt <= _dateTime.UtcNow)
        {
            throw new ConflictException("Час на оплату цього бронювання вичерпано. Оберіть місця повторно.");
        }

        // Якщо оплату вже було ініційовано раніше (напр. користувач оновив сторінку) — не плодимо записи Payment,
        // а повертаємо нову платіжну сесію для того самого запису.
        var payment = booking.Payment ?? new Payment
        {
            BookingId = booking.Id,
            Amount = booking.TotalPrice,
            Method = PaymentMethod.ONLINE,
            Status = PaymentStatus.PENDING
        };

        var session = await _paymentGateway.CreatePaymentSessionAsync(
            payment.Id, booking.TotalPrice, $"CineVerse: бронювання {booking.Id}", cancellationToken);

        payment.TransactionId = session.GatewayTransactionId;

        if (booking.Payment is null)
        {
            _context.Payments.Add(payment);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new PaymentInitiationResultDto(payment.Id, session.RedirectUrl);
    }
}
