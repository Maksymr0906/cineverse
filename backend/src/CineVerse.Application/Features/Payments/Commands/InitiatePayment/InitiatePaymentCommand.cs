using CineVerse.Application.Features.Payments.Common;
using MediatR;

namespace CineVerse.Application.Features.Payments.Commands.InitiatePayment;

/// <summary>Створює запис оплати для PENDING-бронювання і повертає посилання на платіжну сторінку.</summary>
public record InitiatePaymentCommand(Guid BookingId, Guid UserId) : IRequest<PaymentInitiationResultDto>;
