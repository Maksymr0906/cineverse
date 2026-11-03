using FluentValidation;

namespace CineVerse.Application.Features.Auth.Commands.ResetPassword;

public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty();

        // Той самий стандарт надійності, що й при реєстрації (RegisterUserCommandValidator).
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(8).WithMessage("Пароль має містити щонайменше 8 символів.")
            .Matches("[A-Z]").WithMessage("Пароль має містити хоча б одну велику літеру.")
            .Matches("[0-9]").WithMessage("Пароль має містити хоча б одну цифру.");
    }
}
