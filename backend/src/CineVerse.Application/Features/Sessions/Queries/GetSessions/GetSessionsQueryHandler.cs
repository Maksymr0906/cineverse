using CineVerse.Application.Common.Interfaces;
using CineVerse.Application.Features.Sessions.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineVerse.Application.Features.Sessions.Queries.GetSessions;

public class GetSessionsQueryHandler : IRequestHandler<GetSessionsQuery, IReadOnlyList<SessionDto>>
{
    // Мережа кінотеатрів CineVerse наразі працює лише в Україні (п.1 ТЗ) — тому "сьогодні"
    // для фільтра розкладу означає добу за київським часом, а не UTC. Якщо колись з'явиться
    // мультирегіональна мережа, це поле стане properties на Cinema, а не константою тут.
    //
    // ВИПРАВЛЕННЯ ПІСЛЯ АУДИТУ: раніше межі доби рахувались напряму в UTC
    // (request.Date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)) — для UTC+2/+3 це означало,
    // що сеанс о 00:30 за Києвом (для глядача це пізній сеанс "сьогодні") потрапляв у фільтр
    // "завтра", і навпаки для перших годин доби. Той самий корінь проблеми, що й на фронтенді
    // (MoviePage раніше рахував "сьогодні" через toISOString(), теж виправлено).
    private static readonly TimeZoneInfo CinemaTimeZone = ResolveCinemaTimeZone();

    private readonly IApplicationDbContext _context;

    public GetSessionsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<SessionDto>> Handle(GetSessionsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Sessions
            .Include(s => s.Movie)
            .Include(s => s.Hall).ThenInclude(h => h.Cinema)
            .AsQueryable();

        if (request.MovieId.HasValue)
        {
            query = query.Where(s => s.MovieId == request.MovieId.Value);
        }

        if (request.CinemaId.HasValue)
        {
            query = query.Where(s => s.Hall.CinemaId == request.CinemaId.Value);
        }

        if (request.Date.HasValue)
        {
            var (utcDayStart, utcDayEnd) = GetUtcDayBounds(request.Date.Value);
            query = query.Where(s => s.StartTime >= utcDayStart && s.StartTime < utcDayEnd);
        }

        var sessions = await query.OrderBy(s => s.StartTime).ToListAsync(cancellationToken);

        return sessions.Select(s => new SessionDto(
            s.Id, s.MovieId, s.Movie.Title, s.Movie.PosterUrl,
            s.HallId, s.Hall.Name, s.Hall.CinemaId, s.Hall.Cinema.Name,
            s.StartTime, s.EndTime, s.Format, s.BasePrice
        )).ToList();
    }

    private static (DateTimeOffset Start, DateTimeOffset End) GetUtcDayBounds(DateOnly localDate)
    {
        var localDayStart = DateTime.SpecifyKind(localDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
        var localDayEnd = localDayStart.AddDays(1);

        var utcStart = TimeZoneInfo.ConvertTimeToUtc(localDayStart, CinemaTimeZone);
        var utcEnd = TimeZoneInfo.ConvertTimeToUtc(localDayEnd, CinemaTimeZone);

        return (new DateTimeOffset(utcStart, TimeSpan.Zero), new DateTimeOffset(utcEnd, TimeSpan.Zero));
    }

    private static TimeZoneInfo ResolveCinemaTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Europe/Kyiv");
        }
        catch (TimeZoneNotFoundException)
        {
            // Захист на випадок, якщо базовий Docker-образ не містить tzdata: краще працювати
            // в UTC (стара поведінка), ніж впасти повністю. Офіційні образи mcr.microsoft.com/dotnet
            // включають tzdata "з коробки", тому на практиці цей catch — лише страховка.
            return TimeZoneInfo.Utc;
        }
    }
}
