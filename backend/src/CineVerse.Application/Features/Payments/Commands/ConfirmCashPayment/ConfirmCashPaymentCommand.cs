using MediatR;

namespace CineVerse.Application.Features.Payments.Commands.ConfirmCashPayment;

/// <summary>Підтвердження оплати готівкою в касі кінотеатру (роль CASHIER, п.2.2 ТЗ).
/// На відміну від онлайн-оплати, тут немає платіжного шлюзу — касир підтверджує факт оплати вручну.</summary>
public record ConfirmCashPaymentCommand(Guid BookingId, Guid CashierUserId) : IRequest;
