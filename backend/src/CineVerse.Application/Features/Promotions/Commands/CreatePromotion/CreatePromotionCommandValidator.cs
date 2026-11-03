using CineVerse.Domain.Enums;
using FluentValidation;

namespace CineVerse.Application.Features.Promotions.Commands.CreatePromotion;

public class CreatePromotionCommandValidator : AbstractValidator<CreatePromotionCommand>
{
    public CreatePromotionCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30);
        RuleFor(x => x.DiscountType).Must(t => Enum.TryParse<DiscountType>(t, true, out _))
            .WithMessage($"Тип знижки має бути одним із: {string.Join(", ", Enum.GetNames<DiscountType>())}");
        RuleFor(x => x.DiscountValue).GreaterThan(0);
        RuleFor(x => x.ExpiresAt).GreaterThan(DateTimeOffset.UtcNow).WithMessage("Дата завершення дії промокоду має бути в майбутньому.");
        RuleFor(x => x.UsageLimit).GreaterThan(0).When(x => x.UsageLimit.HasValue);
    }
}
