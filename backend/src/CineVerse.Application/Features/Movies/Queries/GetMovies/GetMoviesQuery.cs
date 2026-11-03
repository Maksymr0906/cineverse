using CineVerse.Application.Features.Movies.Common;
using MediatR;

namespace CineVerse.Application.Features.Movies.Queries.GetMovies;

/// <summary>Список фільмів з фільтрами каталогу (п.2.1 ТЗ): статус, жанр, пошук за назвою, пагінація.</summary>
public record GetMoviesQuery(
    string? Status = null,
    Guid? GenreId = null,
    string? Search = null,
    int PageNumber = 1,
    int PageSize = 20
) : IRequest<PagedResult<MovieListItemDto>>;

public record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int PageNumber, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
