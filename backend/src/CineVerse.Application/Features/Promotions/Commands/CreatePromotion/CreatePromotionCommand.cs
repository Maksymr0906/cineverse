using MediatR;

namespace CineVerse.Application.Features.Promotions.Commands.CreatePromotion;

/// <summary>Створення промокоду адміністратором (п.2.1 ТЗ, сценарій "STUDENT20" — знижка 20%).</summary>
public record CreatePromotionCommand(string Code, string DiscountType, decimal DiscountValue, DateTimeOffset ExpiresAt, int? UsageLimit) : IRequest<Guid>;
