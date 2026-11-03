using CineVerse.Domain.Enums;
using FluentValidation;

namespace CineVerse.Application.Features.Movies.Commands.CreateMovie;

public class CreateMovieCommandValidator : AbstractValidator<CreateMovieCommand>
{
    public CreateMovieCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Duration).InclusiveBetween(1, 600).WithMessage("Тривалість має бути від 1 до 600 хвилин.");
        RuleFor(x => x.AgeRating).NotEmpty().MaximumLength(10);
        RuleFor(x => x.Status)
            .Must(s => Enum.TryParse<MovieStatus>(s, true, out _))
            .WithMessage($"Статус має бути одним із: {string.Join(", ", Enum.GetNames<MovieStatus>())}");
        RuleFor(x => x.GenreIds).NotEmpty().WithMessage("Оберіть хоча б один жанр.");
    }
}
