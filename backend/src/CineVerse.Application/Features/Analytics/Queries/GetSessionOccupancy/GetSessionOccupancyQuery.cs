using MediatR;

namespace CineVerse.Application.Features.Analytics.Queries.GetSessionOccupancy;

public record SessionOccupancyDto(
    Guid SessionId, string MovieTitle, string HallName, string CinemaName,
    DateTimeOffset StartTime, int Capacity, int SoldSeats, double OccupancyPercent
);

/// <summary>Заповнюваність майбутніх/недавніх сеансів — другий елемент мінімального
/// дашборду аналітики.</summary>
public record GetSessionOccupancyQuery(int UpcomingDays = 7) : IRequest<IReadOnlyList<SessionOccupancyDto>>;
