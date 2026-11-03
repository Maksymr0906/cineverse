using CineVerse.Application.Common.Exceptions;
using CineVerse.Application.Common.Interfaces;
using CineVerse.Domain.Entities;
using CineVerse.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineVerse.Application.Features.Sessions.Commands.DeleteSession;

public class DeleteSessionCommandHandler : IRequestHandler<DeleteSessionCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteSessionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await _context.Sessions.FirstOrDefaultAsync(s => s.Id == request.SessionId, cancellationToken)
            ?? throw new NotFoundException(nameof(Session), request.SessionId);

        var hasConfirmedBookings = await _context.Bookings
            .AnyAsync(b => b.SessionId == session.Id && b.Status == BookingStatus.CONFIRMED, cancellationToken);
        if (hasConfirmedBookings)
        {
            throw new ConflictException("Неможливо видалити сеанс: на нього вже є підтверджені бронювання. Спочатку скасуйте/поверніть кошти.");
        }

        _context.Sessions.Remove(session);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
