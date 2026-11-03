using FluentValidation;

namespace CineVerse.Application.Features.Reviews.Commands.CreateReview;

public class CreateReviewCommandValidator : AbstractValidator<CreateReviewCommand>
{
    public CreateReviewCommandValidator()
    {
        RuleFor(x => x.Rating).InclusiveBetween(1, 5).WithMessage("Оцінка має бути від 1 до 5.");
        RuleFor(x => x.Comment).MaximumLength(2000);
    }
}
