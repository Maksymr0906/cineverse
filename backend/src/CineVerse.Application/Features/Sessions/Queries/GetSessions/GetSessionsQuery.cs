using CineVerse.Application.Features.Sessions.Common;
using MediatR;

namespace CineVerse.Application.Features.Sessions.Queries.GetSessions;

/// <summary>Розклад сеансів з фільтрами (п.2.1 ТЗ: фільтр за фільмом/кінотеатром/датою).</summary>
public record GetSessionsQuery(
    Guid? MovieId = null,
    Guid? CinemaId = null,
    DateOnly? Date = null
) : IRequest<IReadOnlyList<SessionDto>>;
