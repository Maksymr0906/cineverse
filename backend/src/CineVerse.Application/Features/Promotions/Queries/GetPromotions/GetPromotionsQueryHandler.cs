using CineVerse.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineVerse.Application.Features.Promotions.Queries.GetPromotions;

public class GetPromotionsQueryHandler : IRequestHandler<GetPromotionsQuery, IReadOnlyList<PromotionDto>>
{
    private readonly IApplicationDbContext _context;

    public GetPromotionsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<PromotionDto>> Handle(GetPromotionsQuery request, CancellationToken cancellationToken)
    {
        return await _context.Promotions
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PromotionDto(p.Id, p.Code, p.DiscountType.ToString(), p.DiscountValue, p.ExpiresAt, p.UsageLimit, p.UsageCount))
            .ToListAsync(cancellationToken);
    }
}
