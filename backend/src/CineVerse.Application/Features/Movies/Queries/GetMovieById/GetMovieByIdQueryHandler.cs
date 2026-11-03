using CineVerse.Application.Common.Exceptions;
using CineVerse.Application.Common.Interfaces;
using CineVerse.Application.Features.Movies.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineVerse.Application.Features.Movies.Queries.GetMovieById;

public class GetMovieByIdQueryHandler : IRequestHandler<GetMovieByIdQuery, MovieDetailsDto>
{
    private readonly IApplicationDbContext _context;

    public GetMovieByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<MovieDetailsDto> Handle(GetMovieByIdQuery request, CancellationToken cancellationToken)
    {
        var movie = await _context.Movies
            .Include(m => m.MovieGenres).ThenInclude(mg => mg.Genre)
            .Include(m => m.MovieActors).ThenInclude(ma => ma.Actor)
            .Include(m => m.Reviews)
            .FirstOrDefaultAsync(m => m.Id == request.MovieId, cancellationToken);

        if (movie is null)
        {
            throw new NotFoundException(nameof(Domain.Entities.Movie), request.MovieId);
        }

        var verifiedReviews = movie.Reviews.Where(r => r.IsVerified).ToList();

        return new MovieDetailsDto(
            movie.Id,
            movie.Title,
            movie.Description,
            movie.Duration,
            movie.ReleaseDate,
            movie.AgeRating,
            movie.PosterUrl,
            movie.TrailerUrl,
            movie.Status.ToString(),
            movie.MovieGenres.Select(mg => mg.Genre.Name).ToList(),
            movie.MovieActors.Select(ma => new ActorDto(ma.Actor.Id, ma.Actor.Name, ma.Actor.PhotoUrl, ma.CharacterName)).ToList(),
            verifiedReviews.Count > 0 ? verifiedReviews.Average(r => r.Rating) : 0,
            verifiedReviews.Count
        );
    }
}
