using CineVerse.Application.Common.Exceptions;
using CineVerse.Application.Common.Interfaces;
using CineVerse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using MediatR;

namespace CineVerse.Application.Features.Movies.Commands.DeleteMovie;

public class DeleteMovieCommandHandler : IRequestHandler<DeleteMovieCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteMovieCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteMovieCommand request, CancellationToken cancellationToken)
    {
        var movie = await _context.Movies.FirstOrDefaultAsync(m => m.Id == request.MovieId, cancellationToken);
        if (movie is null)
        {
            throw new NotFoundException(nameof(Movie), request.MovieId);
        }

        var hasSessions = await _context.Sessions.AnyAsync(s => s.MovieId == request.MovieId, cancellationToken);
        if (hasSessions)
        {
            // Бізнес-правило: фільм із прив'язаними сеансами не видаляється фізично,
            // щоб не зламати історію бронювань/квитків — лише переводиться в статус ENDED.
            throw new ConflictException("Неможливо видалити фільм із наявними сеансами. Переведіть статус у ENDED замість видалення.");
        }

        _context.Movies.Remove(movie);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
