using MediatR;

namespace CineVerse.Application.Features.Analytics.Queries.GetRevenueByDay;

public record RevenueByDayDto(DateOnly Date, decimal Revenue, int BookingsCount);

/// <summary>Виручка за останні N днів (за CONFIRMED-бронюваннями) — мінімальний дашборд
/// аналітики для кабінету адміністратора, п. "бажано" аудиту.</summary>
public record GetRevenueByDayQuery(int Days = 14) : IRequest<IReadOnlyList<RevenueByDayDto>>;
