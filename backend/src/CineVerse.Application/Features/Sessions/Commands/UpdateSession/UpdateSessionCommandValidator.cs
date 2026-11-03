using FluentValidation;

namespace CineVerse.Application.Features.Sessions.Commands.UpdateSession;

public class UpdateSessionCommandValidator : AbstractValidator<UpdateSessionCommand>
{
    public UpdateSessionCommandValidator()
    {
        RuleFor(x => x.Format).NotEmpty().MaximumLength(20);
        RuleFor(x => x.BasePrice).GreaterThan(0);

        // Виправлення після аудиту: цього правила бракувало тут, хоча воно є в CreateSessionCommandValidator —
        // без нього адміністратор міг через PUT /sessions/{id} перенести вже існуючий сеанс у минуле.
        RuleFor(x => x.StartTime).GreaterThan(DateTimeOffset.UtcNow)
            .WithMessage("Сеанс не можна перенести в минуле.");
    }
}
