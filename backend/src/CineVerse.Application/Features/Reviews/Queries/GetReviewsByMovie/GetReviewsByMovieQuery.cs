using CineVerse.Application.Features.Reviews.Common;
using MediatR;

namespace CineVerse.Application.Features.Reviews.Queries.GetReviewsByMovie;

public record GetReviewsByMovieQuery(Guid MovieId) : IRequest<IReadOnlyList<ReviewDto>>;
