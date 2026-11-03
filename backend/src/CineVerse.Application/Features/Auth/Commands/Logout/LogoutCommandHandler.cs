using CineVerse.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineVerse.Application.Features.Auth.Commands.Logout;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand>
{
    private readonly IApplicationDbContext _context;

    public LogoutCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user is null) return; // ідемпотентно: вихід із неіснуючого/вже вийшлого користувача не є помилкою

        user.RefreshToken = null;
        user.RefreshTokenExpiresAt = null;
        await _context.SaveChangesAsync(cancellationToken);
    }
}
