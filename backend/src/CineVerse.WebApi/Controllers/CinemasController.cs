using CineVerse.Application.Features.Cinemas.Commands.CreateCinema;
using CineVerse.Application.Features.Cinemas.Commands.CreateHall;
using CineVerse.Application.Features.Cinemas.Commands.UpdateHall;
using CineVerse.Application.Features.Cinemas.Queries.GetCinemas;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CineVerse.WebApi.Controllers;

[ApiController]
[Route("api/cinemas")]
public class CinemasController : ControllerBase
{
    private readonly IMediator _mediator;

    public CinemasController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Список кінотеатрів мережі; ?city=... для фільтра вибору міста на Головній сторінці.</summary>
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetCinemas([FromQuery] string? city, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetCinemasQuery(city), cancellationToken));

    [Authorize(Policy = "AdminOnly")]
    [HttpPost]
    public async Task<IActionResult> CreateCinema([FromBody] CreateCinemaCommand command, CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(command, cancellationToken);
        return Ok(new { id });
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPost("halls")]
    public async Task<IActionResult> CreateHall([FromBody] CreateHallCommand command, CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(command, cancellationToken);
        return Ok(new { id });
    }

    /// <summary>Редагування залу — лише назва й тип; розсадка після створення не змінюється
    /// (див. коментар у UpdateHallCommand).</summary>
    [Authorize(Policy = "AdminOnly")]
    [HttpPut("halls/{id:guid}")]
    public async Task<IActionResult> UpdateHall(Guid id, [FromBody] UpdateHallCommand command, CancellationToken cancellationToken)
    {
        if (id != command.HallId) return BadRequest(new { message = "Id у маршруті та тілі запиту не збігаються." });
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }
}
