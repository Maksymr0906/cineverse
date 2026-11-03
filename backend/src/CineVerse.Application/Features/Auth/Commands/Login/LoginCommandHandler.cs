using CineVerse.Application.Common.Exceptions;
using CineVerse.Application.Common.Interfaces;
using CineVerse.Application.Features.Auth.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineVerse.Application.Features.Auth.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResultDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IDateTime _dateTime;

    public LoginCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IDateTime dateTime)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _dateTime = dateTime;
    }

    public async Task<AuthResultDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        // Навмисно однакове повідомлення для "немає такого email" і "невірний пароль",
        // щоб не давати зловмиснику підказку про існування акаунту (user enumeration).
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new AuthenticationException("Невірний email або пароль.");
        }

        if (!user.IsEmailConfirmed)
        {
            throw new AuthenticationException("Email не підтверджено. Перевірте пошту для завершення реєстрації.");
        }

        var tokens = _jwtTokenService.GenerateTokenPair(user.Id, user.Email, user.Role.ToString());

        user.RefreshToken = tokens.RefreshToken;
        user.RefreshTokenExpiresAt = tokens.RefreshTokenExpiresAt;
        await _context.SaveChangesAsync(cancellationToken);

        return new AuthResultDto(
            tokens.AccessToken,
            tokens.RefreshToken,
            tokens.RefreshTokenExpiresAt,
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email,
            user.Role.ToString());
    }
}
