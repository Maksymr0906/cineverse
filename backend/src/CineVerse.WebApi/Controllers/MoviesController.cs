using CineVerse.Application.Features.Movies.Commands.CreateMovie;
using CineVerse.Application.Features.Movies.Commands.DeleteMovie;
using CineVerse.Application.Features.Movies.Commands.UpdateMovie;
using CineVerse.Application.Features.Movies.Queries.GetMovieById;
using CineVerse.Application.Features.Movies.Queries.GetMovies;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CineVerse.WebApi.Controllers;

[ApiController]
[Route("api/movies")]
public class MoviesController : ControllerBase
{
    private readonly IMediator _mediator;

    public MoviesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Каталог фільмів з фільтрами: ?status=NOW_SHOWING&genreId=...&search=...&pageNumber=1&pageSize=20</summary>
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetMovies([FromQuery] GetMoviesQuery query, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(query, cancellationToken));

    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetMovieById(Guid id, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetMovieByIdQuery(id), cancellationToken));

    [Authorize(Policy = "StaffOnly")]
    [HttpPost]
    public async Task<IActionResult> CreateMovie([FromBody] CreateMovieCommand command, CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetMovieById), new { id }, new { id });
    }

    [Authorize(Policy = "StaffOnly")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateMovie(Guid id, [FromBody] UpdateMovieCommand command, CancellationToken cancellationToken)
    {
        if (id != command.MovieId) return BadRequest(new { message = "Id у маршруті та тілі запиту не збігаються." });
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [Authorize(Policy = "StaffOnly")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteMovie(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteMovieCommand(id), cancellationToken);
        return NoContent();
    }
}
