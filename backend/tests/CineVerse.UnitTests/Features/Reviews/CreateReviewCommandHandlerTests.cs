using CineVerse.Application.Common.Exceptions;
using CineVerse.Application.Features.Reviews.Commands.CreateReview;
using CineVerse.Domain.Entities;
using CineVerse.Domain.Enums;
using CineVerse.UnitTests.Common;
using FluentAssertions;
using Xunit;

namespace CineVerse.UnitTests.Features.Reviews;

/// <summary>
/// Перевіряє уточнене (за результатами аудиту) правило: відгук можна залишити ЛИШЕ
/// маючи квиток зі статусом USED на цей фільм — саме це і робить "верифікований відгук"
/// (термін із глосарію ТЗ, п.5.2) гарантованим за побудовою, а не окремим слабшим шляхом.
/// </summary>
public class CreateReviewCommandHandlerTests
{
    private static Ticket SeedUsedTicket(CineVerse.Infrastructure.Persistence.ApplicationDbContext context, Guid userId, Guid movieId, Guid sessionId, Guid seatId, TicketStatus status)
    {
        var booking = new Booking { UserId = userId, SessionId = sessionId, Status = BookingStatus.CONFIRMED, TotalPrice = 150, ExpiresAt = DateTimeOffset.UtcNow };
        context.Bookings.Add(booking);

        var ticket = new Ticket
        {
            BookingId = booking.Id, SessionId = sessionId, SeatId = seatId,
            TicketCode = $"CV-{Guid.NewGuid():N}", QrCode = "fake", Status = status
        };
        context.Tickets.Add(ticket);
        context.SaveChanges();
        return ticket;
    }

    [Fact]
    public async Task Handle_UserWithUsedTicket_CreatesVerifiedReview()
    {
        using var db = new TestDbContext();
        var (session, seats) = TestDataBuilder.SeedSessionWithSeats(db.Context, DateTimeOffset.UtcNow.AddDays(-1));
        var user = TestDataBuilder.SeedUser(db.Context);
        SeedUsedTicket(db.Context, user.Id, session.MovieId, session.Id, seats[0].Id, TicketStatus.USED);

        var handler = new CreateReviewCommandHandler(db.Context);
        var reviewId = await handler.Handle(new CreateReviewCommand(user.Id, session.MovieId, 5, "Чудовий фільм!"), CancellationToken.None);

        var review = db.Context.Reviews.Single(r => r.Id == reviewId);
        review.IsVerified.Should().BeTrue();
        review.Rating.Should().Be(5);
    }

    [Fact]
    public async Task Handle_UserWithOnlyActiveTicket_ThrowsConflict()
    {
        using var db = new TestDbContext();
        var (session, seats) = TestDataBuilder.SeedSessionWithSeats(db.Context, DateTimeOffset.UtcNow.AddDays(1));
        var user = TestDataBuilder.SeedUser(db.Context);
        // Квиток куплено, але сеанс ще не відбувся — контролер його не сканував (ACTIVE, не USED).
        SeedUsedTicket(db.Context, user.Id, session.MovieId, session.Id, seats[0].Id, TicketStatus.ACTIVE);

        var handler = new CreateReviewCommandHandler(db.Context);
        var act = async () => await handler.Handle(new CreateReviewCommand(user.Id, session.MovieId, 5, "..."), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>().WithMessage("*переглянувши*");
    }

    [Fact]
    public async Task Handle_UserWithoutAnyTicket_ThrowsConflict()
    {
        using var db = new TestDbContext();
        var (session, _) = TestDataBuilder.SeedSessionWithSeats(db.Context, DateTimeOffset.UtcNow.AddDays(1));
        var user = TestDataBuilder.SeedUser(db.Context);

        var handler = new CreateReviewCommandHandler(db.Context);
        var act = async () => await handler.Handle(new CreateReviewCommand(user.Id, session.MovieId, 3, "..."), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_DuplicateReview_ThrowsConflict()
    {
        using var db = new TestDbContext();
        var (session, seats) = TestDataBuilder.SeedSessionWithSeats(db.Context, DateTimeOffset.UtcNow.AddDays(-1));
        var user = TestDataBuilder.SeedUser(db.Context);
        SeedUsedTicket(db.Context, user.Id, session.MovieId, session.Id, seats[0].Id, TicketStatus.USED);

        var handler = new CreateReviewCommandHandler(db.Context);
        await handler.Handle(new CreateReviewCommand(user.Id, session.MovieId, 5, "Перший відгук"), CancellationToken.None);

        var act = async () => await handler.Handle(new CreateReviewCommand(user.Id, session.MovieId, 4, "Другий відгук"), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>().WithMessage("*вже залишали*");
    }
}
