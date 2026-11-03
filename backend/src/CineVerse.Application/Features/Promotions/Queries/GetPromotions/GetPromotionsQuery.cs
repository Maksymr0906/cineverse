using MediatR;

namespace CineVerse.Application.Features.Promotions.Queries.GetPromotions;

public record PromotionDto(Guid Id, string Code, string DiscountType, decimal DiscountValue, DateTimeOffset ExpiresAt, int? UsageLimit, int UsageCount);

public record GetPromotionsQuery : IRequest<IReadOnlyList<PromotionDto>>;
