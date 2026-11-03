using CineVerse.Application.Features.Promotions.Commands.CreatePromotion;
using CineVerse.Application.Features.Promotions.Queries.GetPromotions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CineVerse.WebApi.Controllers;

[ApiController]
[Route("api/promotions")]
[Authorize(Policy = "AdminOnly")]
public class PromotionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PromotionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetPromotions(CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetPromotionsQuery(), cancellationToken));

    [HttpPost]
    public async Task<IActionResult> CreatePromotion([FromBody] CreatePromotionCommand command, CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(command, cancellationToken);
        return Ok(new { id });
    }
}
