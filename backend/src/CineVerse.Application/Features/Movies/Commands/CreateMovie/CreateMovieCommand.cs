using MediatR;

namespace CineVerse.Application.Features.Movies.Commands.CreateMovie;

/// <summary>Додавання фільму до каталогу (кабінет адміністратора/менеджера, п.2.5 ТЗ).</summary>
public record CreateMovieCommand(
    string Title,
    string Description,
    int Duration,
    DateOnly ReleaseDate,
    string AgeRating,
    string PosterUrl,
    string TrailerUrl,
    string Status,
    IReadOnlyList<Guid> GenreIds,
    IReadOnlyList<CreateMovieActorInput> Actors
) : IRequest<Guid>;

public record CreateMovieActorInput(Guid ActorId, string CharacterName);
