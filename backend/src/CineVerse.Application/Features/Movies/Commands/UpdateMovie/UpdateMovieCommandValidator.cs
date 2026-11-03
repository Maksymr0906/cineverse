using CineVerse.Domain.Enums;
using FluentValidation;

namespace CineVerse.Application.Features.Movies.Commands.UpdateMovie;

public class UpdateMovieCommandValidator : AbstractValidator<UpdateMovieCommand>
{
    public UpdateMovieCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Duration).InclusiveBetween(1, 600);
        RuleFor(x => x.Status).Must(s => Enum.TryParse<MovieStatus>(s, true, out _));
        RuleFor(x => x.GenreIds).NotEmpty();
    }
}
