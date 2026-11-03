using FluentValidation;

namespace CineVerse.Application.Features.Sessions.Commands.CreateSession;

public class CreateSessionCommandValidator : AbstractValidator<CreateSessionCommand>
{
    public CreateSessionCommandValidator()
    {
        RuleFor(x => x.Format).NotEmpty().MaximumLength(20);
        RuleFor(x => x.BasePrice).GreaterThan(0);
        RuleFor(x => x.StartTime).GreaterThan(DateTimeOffset.UtcNow)
            .WithMessage("Сеанс не можна створити в минулому.");
    }
}
