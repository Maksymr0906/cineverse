using CineVerse.Application.Common.Interfaces;
using CineVerse.Application.Features.Tickets.Commands.ValidateTicket;
using CineVerse.Application.Features.Tickets.Queries.GetTicketByCode;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CineVerse.WebApi.Controllers;

public record ValidateTicketRequest(string TicketCode);

/// <summary>
/// Кабінет контролера (PWA, п.2.2 ТЗ): сканування QR-коду на вході в зал у два кроки —
/// 1) GET за кодом показує деталі квитка (кого впускаємо), 2) POST /validate фіксує прохід.
/// Розділення на два кроки — свідоме рішення: одне натискання камерою не повинно одразу
/// й безповоротно "спалювати" квиток, якщо контролер помилково відсканував не той QR.
/// </summary>
[ApiController]
[Route("api/tickets")]
[Authorize(Policy = "CheckerOnly")]
public class TicketsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    public TicketsController(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    /// <summary>Крок 1: показати деталі квитка за кодом із QR, без зміни статусу.</summary>
    [HttpGet("{ticketCode}")]
    public async Task<IActionResult> GetByCode(string ticketCode, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetTicketByCodeQuery(ticketCode), cancellationToken));

    /// <summary>Крок 2: підтвердити прохід — незворотно позначає квиток як USED.</summary>
    [HttpPost("validate")]
    public async Task<IActionResult> Validate([FromBody] ValidateTicketRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ValidateTicketCommand(request.TicketCode, _currentUser.UserId!.Value), cancellationToken);
        return Ok(result);
    }
}
