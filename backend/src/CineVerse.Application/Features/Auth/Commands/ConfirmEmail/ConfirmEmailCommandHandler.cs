using CineVerse.Application.Common.Exceptions;
using CineVerse.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineVerse.Application.Features.Auth.Commands.ConfirmEmail;

public class ConfirmEmailCommandHandler : IRequestHandler<ConfirmEmailCommand>
{
    private readonly IApplicationDbContext _context;

    public ConfirmEmailCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.EmailConfirmationToken == request.Token, cancellationToken);

        if (user is null)
        {
            throw new NotFoundException("Токен підтвердження email недійсний або вже використаний.");
        }

        user.IsEmailConfirmed = true;
        user.EmailConfirmationToken = null;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
