using MediatR;

namespace CineVerse.Application.Features.Genres.Queries.GetGenres;

public record GenreDto(Guid Id, string Name);

public record GetGenresQuery : IRequest<IReadOnlyList<GenreDto>>;
