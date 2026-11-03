using FluentValidation;

namespace CineVerse.Application.Features.Tickets.Commands.ValidateTicket;

public class ValidateTicketCommandValidator : AbstractValidator<ValidateTicketCommand>
{
    public ValidateTicketCommandValidator()
    {
        RuleFor(x => x.TicketCode).NotEmpty();
    }
}
