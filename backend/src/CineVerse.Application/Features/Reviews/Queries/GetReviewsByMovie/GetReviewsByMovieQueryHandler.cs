using CineVerse.Application.Common.Interfaces;
using CineVerse.Application.Features.Reviews.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineVerse.Application.Features.Reviews.Queries.GetReviewsByMovie;

public class GetReviewsByMovieQueryHandler : IRequestHandler<GetReviewsByMovieQuery, IReadOnlyList<ReviewDto>>
{
    private readonly IApplicationDbContext _context;

    public GetReviewsByMovieQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ReviewDto>> Handle(GetReviewsByMovieQuery request, CancellationToken cancellationToken)
    {
        return await _context.Reviews
            .Include(r => r.User)
            .Where(r => r.MovieId == request.MovieId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ReviewDto(r.Id, r.User.FirstName, r.Rating, r.Comment, r.IsVerified, r.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
