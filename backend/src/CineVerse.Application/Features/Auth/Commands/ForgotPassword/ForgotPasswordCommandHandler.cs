using CineVerse.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineVerse.Application.Features.Auth.Commands.ForgotPassword;

/// <summary>
/// Ініціює відновлення пароля. НАВМИСНО завжди "успішно" завершується з точки зору відповіді
/// клієнту незалежно від того, чи існує такий email у системі (User Enumeration protection —
/// той самий принцип, що й у LoginCommandHandler: не підказуємо зловмиснику, які email
/// зареєстровані в системі, аналізуючи різницю у відповідях).
/// </summary>
public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IDateTime _dateTime;

    public ForgotPasswordCommandHandler(IApplicationDbContext context, IEmailService emailService, IDateTime dateTime)
    {
        _context = context;
        _emailService = emailService;
        _dateTime = dateTime;
    }

    public async Task Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);
        if (user is null)
        {
            return; // мовчки — див. коментар до класу
        }

        user.PasswordResetToken = Guid.NewGuid().ToString("N");
        user.PasswordResetTokenExpiresAt = _dateTime.UtcNow.AddMinutes(30);
        await _context.SaveChangesAsync(cancellationToken);

        await _emailService.SendPasswordResetAsync(user.Email, user.PasswordResetToken, cancellationToken);
    }
}
