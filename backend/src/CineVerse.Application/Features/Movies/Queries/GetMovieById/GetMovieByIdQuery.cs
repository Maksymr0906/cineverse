using CineVerse.Application.Features.Movies.Common;
using MediatR;

namespace CineVerse.Application.Features.Movies.Queries.GetMovieById;

public record GetMovieByIdQuery(Guid MovieId) : IRequest<MovieDetailsDto>;
