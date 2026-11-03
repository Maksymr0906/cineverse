using CineVerse.Application.Common.Exceptions;
using CineVerse.Application.Common.Interfaces;
using CineVerse.Application.Features.Sessions.Commands.CreateSession;
using CineVerse.Application.Features.Sessions.Common;
using CineVerse.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineVerse.Application.Features.Sessions.Commands.UpdateSession;

public class UpdateSessionCommandHandler : IRequestHandler<UpdateSessionCommand>
{
    private readonly IApplicationDbContext _context;

    public UpdateSessionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(UpdateSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await _context.Sessions
            .Include(s => s.Movie)
            .FirstOrDefaultAsync(s => s.Id == request.SessionId, cancellationToken)
            ?? throw new NotFoundException(nameof(Session), request.SessionId);

        // Бізнес-правило: сеанс, на який уже є підтверджені бронювання, не можна переносити —
        // це б зламало вже продані квитки. Перенесення дозволене лише для "порожнього" сеансу.
        var hasConfirmedBookings = await _context.Bookings
            .AnyAsync(b => b.SessionId == session.Id && b.Status == Domain.Enums.BookingStatus.CONFIRMED, cancellationToken);
        if (hasConfirmedBookings)
        {
            throw new ConflictException("Неможливо змінити сеанс: на нього вже є підтверджені бронювання.");
        }

        var endTime = request.StartTime
            .AddMinutes(session.Movie.Duration)
            .AddMinutes(SessionScheduling.CleaningBufferMinutes);

        await CreateSessionCommandHandler.EnsureNoScheduleConflictAsync(
            request.HallId, request.StartTime, endTime, excludeSessionId: session.Id, _context, cancellationToken);

        session.HallId = request.HallId;
        session.StartTime = request.StartTime;
        session.EndTime = endTime;
        session.Format = request.Format;
        session.BasePrice = request.BasePrice;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
