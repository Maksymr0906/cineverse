using MediatR;

namespace CineVerse.Application.Features.Movies.Commands.UpdateMovie;

/// <summary>Редагування картки фільму, включно зі зміною статусу (COMING_SOON → NOW_SHOWING → ENDED).</summary>
public record UpdateMovieCommand(
    Guid MovieId,
    string Title,
    string Description,
    int Duration,
    DateOnly ReleaseDate,
    string AgeRating,
    string PosterUrl,
    string TrailerUrl,
    string Status,
    IReadOnlyList<Guid> GenreIds
) : IRequest;
