using CineVerse.Application.Common.Exceptions;
using CineVerse.Application.Common.Interfaces;
using CineVerse.Domain.Entities;
using CineVerse.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineVerse.Application.Features.Reviews.Commands.CreateReview;

/// <summary>
/// Уточнено (термін "верифікований відгук" окремо визначений у глосарії ТЗ, п.5.2):
/// раніше право залишити відгук перевірялось лише за наявності ПІДТВЕРДЖЕНОГО бронювання
/// (CONFIRMED), а "верифікованість" — окремим, слабшим критерієм (є хоч один USED-квиток).
/// Це давало прогалину: людина могла купити квиток і не прийти, залишивши НЕверифікований
/// відгук про фільм, якого не бачила. Тепер сам допуск до написання відгуку вимагає
/// ФАКТИЧНОГО відвідування (є USED-квиток на цей фільм) — тому кожен збережений відгук
/// уже гарантовано верифікований за визначенням, без окремого "слабшого" шляху.
/// </summary>
public class CreateReviewCommandHandler : IRequestHandler<CreateReviewCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public CreateReviewCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateReviewCommand request, CancellationToken cancellationToken)
    {
        var movieExists = await _context.Movies.AnyAsync(m => m.Id == request.MovieId, cancellationToken);
        if (!movieExists)
        {
            throw new NotFoundException(nameof(Movie), request.MovieId);
        }

        var hasUsedTicket = await _context.Tickets
            .AnyAsync(t => t.Session.MovieId == request.MovieId && t.Booking.UserId == request.UserId && t.Status == TicketStatus.USED, cancellationToken);

        if (!hasUsedTicket)
        {
            throw new ConflictException(
                "Залишати відгук можна лише переглянувши фільм — потрібен відсканований на вході квиток (USED). " +
                "Придбаного, але не використаного квитка недостатньо.");
        }

        var alreadyReviewed = await _context.Reviews.AnyAsync(r => r.UserId == request.UserId && r.MovieId == request.MovieId, cancellationToken);
        if (alreadyReviewed)
        {
            throw new ConflictException("Ви вже залишали відгук на цей фільм.");
        }

        var review = new Review
        {
            UserId = request.UserId,
            MovieId = request.MovieId,
            Rating = request.Rating,
            Comment = request.Comment,
            IsVerified = true // гарантовано за побудовою — див. коментар до класу
        };

        _context.Reviews.Add(review);
        await _context.SaveChangesAsync(cancellationToken);

        return review.Id;
    }
}
