using CineVerse.Application.Features.Analytics.Queries.GetRevenueByDay;
using CineVerse.Application.Features.Analytics.Queries.GetSessionOccupancy;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CineVerse.WebApi.Controllers;

/// <summary>Мінімальний дашборд аналітики для кабінету адміністратора (п. "бажано" аудиту):
/// виручка за день і заповнюваність сеансів на вже наявних даних, без окремого сховища звітів.</summary>
[ApiController]
[Route("api/analytics")]
[Authorize(Policy = "StaffOnly")]
public class AnalyticsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AnalyticsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("revenue-by-day")]
    public async Task<IActionResult> GetRevenueByDay([FromQuery] int days, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetRevenueByDayQuery(days == 0 ? 14 : days), cancellationToken));

    [HttpGet("session-occupancy")]
    public async Task<IActionResult> GetSessionOccupancy([FromQuery] int upcomingDays, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetSessionOccupancyQuery(upcomingDays == 0 ? 7 : upcomingDays), cancellationToken));
}
