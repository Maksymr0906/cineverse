using MediatR;

namespace CineVerse.Application.Features.Movies.Commands.DeleteMovie;

public record DeleteMovieCommand(Guid MovieId) : IRequest;
