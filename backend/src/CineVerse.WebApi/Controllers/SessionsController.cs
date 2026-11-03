using CineVerse.Application.Features.Sessions.Commands.CreateSession;
using CineVerse.Application.Features.Sessions.Commands.DeleteSession;
using CineVerse.Application.Features.Sessions.Commands.UpdateSession;
using CineVerse.Application.Features.Sessions.Queries.GetSessions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CineVerse.WebApi.Controllers;

[ApiController]
[Route("api/sessions")]
public class SessionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public SessionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Розклад сеансів: ?movieId=...&cinemaId=...&date=2026-09-10</summary>
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetSessions([FromQuery] GetSessionsQuery query, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(query, cancellationToken));

    /// <summary>Додати сеанс до розкладу. Повертає 409, якщо є накладка з іншим сеансом у тому ж залі.</summary>
    [Authorize(Policy = "StaffOnly")]
    [HttpPost]
    public async Task<IActionResult> CreateSession([FromBody] CreateSessionCommand command, CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(command, cancellationToken);
        return Ok(new { id });
    }

    [Authorize(Policy = "StaffOnly")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateSession(Guid id, [FromBody] UpdateSessionCommand command, CancellationToken cancellationToken)
    {
        if (id != command.SessionId) return BadRequest(new { message = "Id у маршруті та тілі запиту не збігаються." });
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [Authorize(Policy = "StaffOnly")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteSession(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteSessionCommand(id), cancellationToken);
        return NoContent();
    }
}
