using CineVerse.Domain.Enums;
using FluentValidation;

namespace CineVerse.Application.Features.Cinemas.Commands.UpdateHall;

public class UpdateHallCommandValidator : AbstractValidator<UpdateHallCommand>
{
    public UpdateHallCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Type).Must(t => Enum.TryParse<HallType>(t, true, out _))
            .WithMessage($"Тип залу має бути одним із: {string.Join(", ", Enum.GetNames<HallType>())}");
    }
}
