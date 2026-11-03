using CineVerse.Application.Common.Exceptions;
using CineVerse.Application.Common.Interfaces;
using CineVerse.Application.Features.Auth.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineVerse.Application.Features.Auth.Commands.RefreshToken;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResultDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IDateTime _dateTime;

    public RefreshTokenCommandHandler(IApplicationDbContext context, IJwtTokenService jwtTokenService, IDateTime dateTime)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
        _dateTime = dateTime;
    }

    public async Task<AuthResultDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.RefreshToken == request.RefreshToken, cancellationToken);

        if (user is null || user.RefreshTokenExpiresAt is null || user.RefreshTokenExpiresAt < _dateTime.UtcNow)
        {
            throw new AuthenticationException("Refresh-токен недійсний або протермінований. Увійдіть повторно.");
        }

        var tokens = _jwtTokenService.GenerateTokenPair(user.Id, user.Email, user.Role.ToString());

        // Ротація refresh-токена: попередній одразу стає непридатним для повторного використання.
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
