using CineVerse.Application.Common.Exceptions;
using CineVerse.Application.Common.Interfaces;
using CineVerse.Domain.Entities;
using CineVerse.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineVerse.Application.Features.Promotions.Commands.CreatePromotion;

public class CreatePromotionCommandHandler : IRequestHandler<CreatePromotionCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public CreatePromotionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreatePromotionCommand request, CancellationToken cancellationToken)
    {
        var normalizedCode = request.Code.Trim().ToUpperInvariant();

        var exists = await _context.Promotions.AnyAsync(p => p.Code.ToUpper() == normalizedCode, cancellationToken);
        if (exists)
        {
            throw new ConflictException($"Промокод \"{normalizedCode}\" вже існує.");
        }

        var promotion = new Promotion
        {
            Code = normalizedCode,
            DiscountType = Enum.Parse<DiscountType>(request.DiscountType, true),
            DiscountValue = request.DiscountValue,
            ExpiresAt = request.ExpiresAt,
            UsageLimit = request.UsageLimit
        };

        _context.Promotions.Add(promotion);
        await _context.SaveChangesAsync(cancellationToken);

        return promotion.Id;
    }
}
