using CineVerse.Application.Common.Exceptions;
using CineVerse.Application.Common.Interfaces;
using CineVerse.Domain.Entities;
using CineVerse.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineVerse.Application.Features.Movies.Commands.UpdateMovie;

public class UpdateMovieCommandHandler : IRequestHandler<UpdateMovieCommand>
{
    private readonly IApplicationDbContext _context;

    public UpdateMovieCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(UpdateMovieCommand request, CancellationToken cancellationToken)
    {
        var movie = await _context.Movies
            .Include(m => m.MovieGenres)
            .FirstOrDefaultAsync(m => m.Id == request.MovieId, cancellationToken);

        if (movie is null)
        {
            throw new NotFoundException(nameof(Movie), request.MovieId);
        }

        movie.Title = request.Title;
        movie.Description = request.Description;
        movie.Duration = request.Duration;
        movie.ReleaseDate = request.ReleaseDate;
        movie.AgeRating = request.AgeRating;
        movie.PosterUrl = request.PosterUrl;
        movie.TrailerUrl = request.TrailerUrl;
        movie.Status = Enum.Parse<MovieStatus>(request.Status, true);

        // Синхронізація жанрів: видаляємо зняті, додаємо нові
        var currentGenreIds = movie.MovieGenres.Select(mg => mg.GenreId).ToHashSet();
        var requestedGenreIds = request.GenreIds.Distinct().ToHashSet();

        foreach (var toRemove in movie.MovieGenres.Where(mg => !requestedGenreIds.Contains(mg.GenreId)).ToList())
        {
            movie.MovieGenres.Remove(toRemove);
        }

        foreach (var toAdd in requestedGenreIds.Except(currentGenreIds))
        {
            movie.MovieGenres.Add(new MovieGenre { MovieId = movie.Id, GenreId = toAdd });
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
