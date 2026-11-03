using FluentValidation;

namespace CineVerse.Application.Features.Auth.Commands.RegisterUser;

public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(50);

        RuleFor(x => x.Email)
            .NotEmpty().MaximumLength(100)
            .EmailAddress().WithMessage("Некоректний формат email.");

        RuleFor(x => x.Phone)
            .NotEmpty().MaximumLength(20)
            .Matches(@"^\+?[0-9]{10,15}$").WithMessage("Некоректний формат телефону.");

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8).WithMessage("Пароль має містити щонайменше 8 символів.")
            .Matches("[A-Z]").WithMessage("Пароль має містити хоча б одну велику літеру.")
            .Matches("[0-9]").WithMessage("Пароль має містити хоча б одну цифру.");
    }
}
