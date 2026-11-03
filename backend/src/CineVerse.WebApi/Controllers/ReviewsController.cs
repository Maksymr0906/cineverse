using CineVerse.Application.Common.Interfaces;
using CineVerse.Application.Features.Reviews.Commands.CreateReview;
using CineVerse.Application.Features.Reviews.Queries.GetReviewsByMovie;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CineVerse.WebApi.Controllers;

public record CreateReviewRequest(int Rating, string Comment);

[ApiController]
[Route("api/movies/{movieId:guid}/reviews")]
public class ReviewsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    public ReviewsController(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetReviews(Guid movieId, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetReviewsByMovieQuery(movieId), cancellationToken));

    /// <summary>Залишити відгук — лише за наявності підтвердженого бронювання на цей фільм.</summary>
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreateReview(Guid movieId, [FromBody] CreateReviewRequest request, CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(new CreateReviewCommand(_currentUser.UserId!.Value, movieId, request.Rating, request.Comment), cancellationToken);
        return Ok(new { id });
    }
}
