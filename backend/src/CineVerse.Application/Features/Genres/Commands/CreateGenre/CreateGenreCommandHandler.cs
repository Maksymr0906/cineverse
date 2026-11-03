using CineVerse.Application.Common.Exceptions;
using CineVerse.Application.Common.Interfaces;
using CineVerse.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineVerse.Application.Features.Genres.Commands.CreateGenre;

public class CreateGenreCommandHandler : IRequestHandler<CreateGenreCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public CreateGenreCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateGenreCommand request, CancellationToken cancellationToken)
    {
        var exists = await _context.Genres.AnyAsync(g => g.Name == request.Name, cancellationToken);
        if (exists)
        {
            throw new ConflictException($"Жанр \"{request.Name}\" вже існує.");
        }

        var genre = new Genre { Name = request.Name };
        _context.Genres.Add(genre);
        await _context.SaveChangesAsync(cancellationToken);
        return genre.Id;
    }
}
