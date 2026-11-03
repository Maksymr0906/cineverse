using CineVerse.Application.Common.Interfaces;
using CineVerse.Application.Features.Bookings.Commands.CancelBooking;
using CineVerse.Application.Features.Bookings.Commands.CreateBooking;
using CineVerse.Application.Features.Bookings.Queries.GetBookingById;
using CineVerse.Application.Features.Bookings.Queries.GetHallSeatMap;
using CineVerse.Application.Features.Bookings.Queries.GetMyBookings;
using CineVerse.Application.Features.Tickets.Queries.GetTicketsByBooking;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CineVerse.WebApi.Controllers;

public record CreateBookingRequest(Guid SessionId, IReadOnlyList<Guid> SeatIds, string? PromoCode = null);

[ApiController]
[Route("api/bookings")]
[Authorize]
public class BookingsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    public BookingsController(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    /// <summary>Схема залу для сеансу з живими статусами місць (FREE/LOCKED/SOLD). Доступна без входу —
    /// щоб можна було переглянути наявність місць до реєстрації.</summary>
    [AllowAnonymous]
    [HttpGet("seat-map/{sessionId:guid}")]
    public async Task<IActionResult> GetSeatMap(Guid sessionId, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetHallSeatMapQuery(sessionId), cancellationToken));

    /// <summary>Ініціює бронювання обраних місць. 409, якщо хоча б одне місце вже зайняте/обирається.</summary>
    [HttpPost]
    public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequest request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId!.Value;
        var result = await _mediator.Send(new CreateBookingCommand(userId, request.SessionId, request.SeatIds, request.PromoCode), cancellationToken);
        return CreatedAtAction(nameof(GetBookingById), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetBookingById(Guid id, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId!.Value;
        var isStaff = _currentUser.Role is "ADMIN" or "MANAGER" or "CASHIER";
        return Ok(await _mediator.Send(new GetBookingByIdQuery(id, userId, isStaff), cancellationToken));
    }

    /// <summary>Список бронювань поточного користувача — сторінка "Особистий кабінет".</summary>
    [HttpGet("mine")]
    public async Task<IActionResult> GetMyBookings(CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetMyBookingsQuery(_currentUser.UserId!.Value), cancellationToken));

    /// <summary>Скасовує ще неоплачене бронювання й одразу звільняє місця для інших.</summary>
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> CancelBooking(Guid id, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId!.Value;
        await _mediator.Send(new CancelBookingCommand(id, userId), cancellationToken);
        return NoContent();
    }

    /// <summary>Електронні квитки з QR-кодами — доступні лише після успішної оплати (Booking.Status == CONFIRMED).</summary>
    [HttpGet("{id:guid}/tickets")]
    public async Task<IActionResult> GetTickets(Guid id, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId!.Value;
        var isStaff = _currentUser.Role is "ADMIN" or "MANAGER" or "CASHIER" or "CHECKER";
        return Ok(await _mediator.Send(new GetTicketsByBookingQuery(id, userId, isStaff), cancellationToken));
    }
}
