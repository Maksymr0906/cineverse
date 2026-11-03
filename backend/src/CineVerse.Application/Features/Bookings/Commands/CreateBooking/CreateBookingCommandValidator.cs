using FluentValidation;

namespace CineVerse.Application.Features.Bookings.Commands.CreateBooking;

public class CreateBookingCommandValidator : AbstractValidator<CreateBookingCommand>
{
    public CreateBookingCommandValidator()
    {
        RuleFor(x => x.SeatIds)
            .NotEmpty().WithMessage("Оберіть хоча б одне місце.")
            .Must(ids => ids.Count <= 10).WithMessage("За одне бронювання можна обрати не більше 10 місць.")
            .Must(ids => ids.Distinct().Count() == ids.Count).WithMessage("У списку є повторювані місця.");

        RuleFor(x => x.PromoCode).MaximumLength(30).When(x => x.PromoCode is not null);
    }
}
