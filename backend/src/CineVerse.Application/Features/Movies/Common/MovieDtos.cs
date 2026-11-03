namespace CineVerse.Application.Features.Movies.Common;

/// <summary>Короткий DTO фільму — для сітки на Головній сторінці / каталозі (п.2.1 ТЗ).</summary>
public record MovieListItemDto(
    Guid Id,
    string Title,
    int Duration,
    DateOnly ReleaseDate,
    string AgeRating,
    string PosterUrl,
    string Status,
    IReadOnlyList<string> Genres
);

/// <summary>Повний DTO фільму — сторінка фільму: опис, актори, найближчі сеанси.</summary>
public record MovieDetailsDto(
    Guid Id,
    string Title,
    string Description,
    int Duration,
    DateOnly ReleaseDate,
    string AgeRating,
    string PosterUrl,
    string TrailerUrl,
    string Status,
    IReadOnlyList<string> Genres,
    IReadOnlyList<ActorDto> Actors,
    double AverageRating,
    int ReviewsCount
);

public record ActorDto(Guid Id, string Name, string? PhotoUrl, string CharacterName);
