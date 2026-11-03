using CineVerse.Application.Common.Interfaces;
using CineVerse.Domain.Entities;
using CineVerse.Domain.Enums;
using MediatR;

namespace CineVerse.Application.Features.Movies.Commands.CreateMovie;

public class CreateMovieCommandHandler : IRequestHandler<CreateMovieCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public CreateMovieCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateMovieCommand request, CancellationToken cancellationToken)
    {
        var movie = new Movie
        {
            Title = request.Title,
            Description = request.Description,
            Duration = request.Duration,
            ReleaseDate = request.ReleaseDate,
            AgeRating = request.AgeRating,
            PosterUrl = request.PosterUrl,
            TrailerUrl = request.TrailerUrl,
            Status = Enum.Parse<MovieStatus>(request.Status, true)
        };

        foreach (var genreId in request.GenreIds.Distinct())
        {
            movie.MovieGenres.Add(new MovieGenre { MovieId = movie.Id, GenreId = genreId });
        }

        foreach (var actor in request.Actors)
        {
            movie.MovieActors.Add(new MovieActor { MovieId = movie.Id, ActorId = actor.ActorId, CharacterName = actor.CharacterName });
        }

        _context.Movies.Add(movie);
        await _context.SaveChangesAsync(cancellationToken);

        return movie.Id;
    }
}
