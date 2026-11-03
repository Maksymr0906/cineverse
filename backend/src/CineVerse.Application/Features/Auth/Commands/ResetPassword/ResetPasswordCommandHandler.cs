using CineVerse.Application.Common.Exceptions;
using CineVerse.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineVerse.Application.Features.Auth.Commands.ResetPassword;

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDateTime _dateTime;

    public ResetPasswordCommandHandler(IApplicationDbContext context, IPasswordHasher passwordHasher, IDateTime dateTime)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _dateTime = dateTime;
    }

    public async Task Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.PasswordResetToken == request.Token, cancellationToken);

        if (user is null || user.PasswordResetTokenExpiresAt is null || user.PasswordResetTokenExpiresAt < _dateTime.UtcNow)
        {
            throw new AuthenticationException("Посилання для відновлення пароля недійсне або протерміноване. Запросіть нове.");
        }

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiresAt = null;

        // Відновлення пароля — розумний привід інвалідувати й активну refresh-сесію:
        // якщо пароль міняли через компрометацію акаунту, стара сесія не повинна лишатись живою.
        user.RefreshToken = null;
        user.RefreshTokenExpiresAt = null;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
