using CineVerse.Application.Common.Interfaces;
using CineVerse.Application.Features.Cinemas.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineVerse.Application.Features.Cinemas.Queries.GetCinemas;

public class GetCinemasQueryHandler : IRequestHandler<GetCinemasQuery, IReadOnlyList<CinemaDto>>
{
    private readonly IApplicationDbContext _context;

    public GetCinemasQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<CinemaDto>> Handle(GetCinemasQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Cinemas.Include(c => c.Halls).AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.City))
        {
            query = query.Where(c => c.City == request.City);
        }

        var cinemas = await query.OrderBy(c => c.Name).ToListAsync(cancellationToken);

        return cinemas.Select(c => new CinemaDto(
            c.Id, c.Name, c.Address, c.City, c.Phone,
            c.Halls.Select(h => new HallDto(h.Id, h.Name, h.Type.ToString(), h.Capacity)).ToList()
        )).ToList();
    }
}
