using CineVerse.Application.Common.Interfaces;
using CineVerse.Application.Features.Movies.Common;
using CineVerse.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineVerse.Application.Features.Movies.Queries.GetMovies;

public class GetMoviesQueryHandler : IRequestHandler<GetMoviesQuery, PagedResult<MovieListItemDto>>
{
    private readonly IApplicationDbContext _context;

    public GetMoviesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<MovieListItemDto>> Handle(GetMoviesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Movies
            .Include(m => m.MovieGenres).ThenInclude(mg => mg.Genre)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Status) && Enum.TryParse<MovieStatus>(request.Status, true, out var status))
        {
            query = query.Where(m => m.Status == status);
        }

        if (request.GenreId.HasValue)
        {
            query = query.Where(m => m.MovieGenres.Any(mg => mg.GenreId == request.GenreId.Value));
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            // Виправлення після аудиту: %  і _ у введенні користувача — це SQL-вайлдкарди ILIKE,
            // а не буквальні символи. Без екранування пошук "100%" чи "Movie_2" давав би несподівано
            // широкі/вузькі результати (не SQL-ін'єкція — параметризація й так коректна, суто логічна помилка).
            var escaped = request.Search.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
            var pattern = $"%{escaped}%";
            query = query.Where(m => EF.Functions.ILike(m.Title, pattern, "\\"));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var pageNumber = Math.Max(request.PageNumber, 1);

        var items = await query
            .OrderByDescending(m => m.ReleaseDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new MovieListItemDto(
                m.Id,
                m.Title,
                m.Duration,
                m.ReleaseDate,
                m.AgeRating,
                m.PosterUrl,
                m.Status.ToString(),
                m.MovieGenres.Select(mg => mg.Genre.Name).ToList()))
            .ToListAsync(cancellationToken);

        return new PagedResult<MovieListItemDto>(items, totalCount, pageNumber, pageSize);
    }
}
