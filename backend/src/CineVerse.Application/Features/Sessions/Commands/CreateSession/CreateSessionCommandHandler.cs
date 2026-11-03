using CineVerse.Application.Common.Exceptions;
using CineVerse.Application.Common.Interfaces;
using CineVerse.Application.Features.Sessions.Common;
using CineVerse.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineVerse.Application.Features.Sessions.Commands.CreateSession;

public class CreateSessionCommandHandler : IRequestHandler<CreateSessionCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public CreateSessionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateSessionCommand request, CancellationToken cancellationToken)
    {
        var movie = await _context.Movies.FirstOrDefaultAsync(m => m.Id == request.MovieId, cancellationToken)
            ?? throw new NotFoundException(nameof(Movie), request.MovieId);

        var hallExists = await _context.Halls.AnyAsync(h => h.Id == request.HallId, cancellationToken);
        if (!hallExists)
        {
            throw new NotFoundException(nameof(Hall), request.HallId);
        }

        var endTime = request.StartTime
            .AddMinutes(movie.Duration)
            .AddMinutes(SessionScheduling.CleaningBufferMinutes);

        await EnsureNoScheduleConflictAsync(request.HallId, request.StartTime, endTime, excludeSessionId: null, cancellationToken);

        var session = new Session
        {
            MovieId = request.MovieId,
            HallId = request.HallId,
            StartTime = request.StartTime,
            EndTime = endTime,
            Format = request.Format,
            BasePrice = request.BasePrice
        };

        _context.Sessions.Add(session);
        await _context.SaveChangesAsync(cancellationToken);

        return session.Id;
    }

    /// <summary>
    /// Перевірка накладки за класичною формулою перетину інтервалів:
    /// existing.Start &lt; newEnd  AND  newStart &lt; existing.End.
    /// Використовується і при створенні, і при редагуванні сеансу (тому винесено статично-придатним для повторного виклику).
    /// </summary>
    internal static async Task EnsureNoScheduleConflictAsync(
        Guid hallId,
        DateTimeOffset newStart,
        DateTimeOffset newEnd,
        Guid? excludeSessionId,
        IApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var conflict = await context.Sessions
            .Where(s => s.HallId == hallId && s.Id != (excludeSessionId ?? Guid.Empty))
            .Where(s => s.StartTime < newEnd && newStart < s.EndTime)
            .Select(s => new { s.Id, s.StartTime, s.EndTime })
            .FirstOrDefaultAsync(cancellationToken);

        if (conflict is not null)
        {
            throw new ConflictException(
                $"Конфлікт розкладу: у цьому залі вже є сеанс з {conflict.StartTime:HH:mm} до {conflict.EndTime:HH:mm} " +
                $"(з урахуванням {SessionScheduling.CleaningBufferMinutes} хв технічної перерви).");
        }
    }

    private Task EnsureNoScheduleConflictAsync(Guid hallId, DateTimeOffset newStart, DateTimeOffset newEnd, Guid? excludeSessionId, CancellationToken cancellationToken)
        => EnsureNoScheduleConflictAsync(hallId, newStart, newEnd, excludeSessionId, _context, cancellationToken);
}
