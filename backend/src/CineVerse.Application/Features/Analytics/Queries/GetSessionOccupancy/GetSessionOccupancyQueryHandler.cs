using CineVerse.Application.Common.Interfaces;
using CineVerse.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineVerse.Application.Features.Analytics.Queries.GetSessionOccupancy;

public class GetSessionOccupancyQueryHandler : IRequestHandler<GetSessionOccupancyQuery, IReadOnlyList<SessionOccupancyDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTime _dateTime;

    public GetSessionOccupancyQueryHandler(IApplicationDbContext context, IDateTime dateTime)
    {
        _context = context;
        _dateTime = dateTime;
    }

    public async Task<IReadOnlyList<SessionOccupancyDto>> Handle(GetSessionOccupancyQuery request, CancellationToken cancellationToken)
    {
        var now = _dateTime.UtcNow;
        var until = now.AddDays(Math.Clamp(request.UpcomingDays, 1, 30));

        var sessions = await _context.Sessions
            .Include(s => s.Movie)
            .Include(s => s.Hall).ThenInclude(h => h.Cinema)
            .Where(s => s.StartTime >= now.AddDays(-1) && s.StartTime <= until) // трохи "в минуле" — щоб бачити щойно завершені сеанси теж
            .OrderBy(s => s.StartTime)
            .ToListAsync(cancellationToken);

        var soldCounts = await _context.BookingSeats
            .Include(bs => bs.Booking)
            .Where(bs => bs.Booking.Status == BookingStatus.CONFIRMED)
            .GroupBy(bs => bs.Booking.SessionId)
            .Select(g => new { SessionId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.SessionId, x => x.Count, cancellationToken);

        return sessions.Select(s =>
        {
            var sold = soldCounts.GetValueOrDefault(s.Id, 0);
            var capacity = s.Hall.Capacity;
            var percent = capacity > 0 ? Math.Round(sold * 100.0 / capacity, 1) : 0;
            return new SessionOccupancyDto(s.Id, s.Movie.Title, s.Hall.Name, s.Hall.Cinema.Name, s.StartTime, capacity, sold, percent);
        }).ToList();
    }
}
