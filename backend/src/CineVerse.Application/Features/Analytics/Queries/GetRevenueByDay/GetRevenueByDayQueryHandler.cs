using CineVerse.Application.Common.Interfaces;
using CineVerse.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineVerse.Application.Features.Analytics.Queries.GetRevenueByDay;

public class GetRevenueByDayQueryHandler : IRequestHandler<GetRevenueByDayQuery, IReadOnlyList<RevenueByDayDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTime _dateTime;

    public GetRevenueByDayQueryHandler(IApplicationDbContext context, IDateTime dateTime)
    {
        _context = context;
        _dateTime = dateTime;
    }

    public async Task<IReadOnlyList<RevenueByDayDto>> Handle(GetRevenueByDayQuery request, CancellationToken cancellationToken)
    {
        var days = Math.Clamp(request.Days, 1, 90);
        var sinceUtc = _dateTime.UtcNow.AddDays(-days);

        // Групуємо по календарній даті створення бронювання (UTC — для простого дашборду
        // "виручка за день" це прийнятна точність; для точнішого звіту варто було б, як і в
        // GetSessionsQueryHandler, рахувати межі доби за часовим поясом кінотеатру).
        var raw = await _context.Bookings
            .Where(b => b.Status == BookingStatus.CONFIRMED && b.CreatedAt >= sinceUtc)
            .GroupBy(b => b.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Revenue = g.Sum(b => b.TotalPrice), Count = g.Count() })
            .ToListAsync(cancellationToken);

        return raw
            .OrderBy(r => r.Date)
            .Select(r => new RevenueByDayDto(DateOnly.FromDateTime(r.Date), r.Revenue, r.Count))
            .ToList();
    }
}
