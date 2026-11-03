using MediatR;

namespace CineVerse.Application.Features.Genres.Commands.CreateGenre;

public record CreateGenreCommand(string Name) : IRequest<Guid>;
