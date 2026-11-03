using CineVerse.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineVerse.Application.Features.Genres.Queries.GetGenres;

public class GetGenresQueryHandler : IRequestHandler<GetGenresQuery, IReadOnlyList<GenreDto>>
{
    private readonly IApplicationDbContext _context;

    public GetGenresQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<GenreDto>> Handle(GetGenresQuery request, CancellationToken cancellationToken)
    {
        return await _context.Genres
            .OrderBy(g => g.Name)
            .Select(g => new GenreDto(g.Id, g.Name))
            .ToListAsync(cancellationToken);
    }
}
