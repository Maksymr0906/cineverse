using CineVerse.Application.Common.Exceptions;
using CineVerse.Application.Common.Interfaces;
using CineVerse.Application.Features.Tickets.Commands.ValidateTicket;
using CineVerse.Domain.Entities;
using CineVerse.Domain.Enums;
using CineVerse.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace CineVerse.UnitTests.Features.Tickets;

public class ValidateTicketCommandHandlerTests
{
    private static readonly DateTimeOffset SessionStart = new(2026, 9, 10, 19, 0, 0, TimeSpan.Zero);

    private static IConfiguration BuildConfig(int earlyEntryMinutes = 30) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["TicketValidation:EarlyEntryMinutes"] = earlyEntryMinutes.ToString() })
            .Build();

    private static Mock<IDateTime> ClockAt(DateTimeOffset now)
    {
        var clock = new Mock<IDateTime>();
        clock.SetupGet(x => x.UtcNow).Returns(now);
        return clock;
    }

    private static Ticket SeedActiveTicket(CineVerse.Infrastructure.Persistence.ApplicationDbContext context, DateTimeOffset sessionStart, out Guid checkerId)
    {
        var (session, seats) = TestDataBuilder.SeedSessionWithSeats(context, sessionStart);
        var user = TestDataBuilder.SeedUser(context);
        var checker = TestDataBuilder.SeedUser(context, UserRole.CHECKER);
        checkerId = checker.Id;

        var booking = new Booking { UserId = user.Id, SessionId = session.Id, Status = BookingStatus.CONFIRMED, TotalPrice = 150, ExpiresAt = sessionStart };
        context.Bookings.Add(booking);

        var ticket = new Ticket
        {
            BookingId = booking.Id,
            SessionId = session.Id,
            SeatId = seats[0].Id,
            TicketCode = "CV-TEST0000001",
            QrCode = "data:image/png;base64,fake",
            Status = TicketStatus.ACTIVE
        };
        context.Tickets.Add(ticket);
        context.SaveChanges();

        return ticket;
    }

    [Fact]
    public async Task Handle_ActiveTicket_WithinEntryWindow_MarksUsed_AndReturnsSeatLabel()
    {
        using var db = new TestDbContext();
        var ticket = SeedActiveTicket(db.Context, SessionStart, out var checkerId);
        var clock = ClockAt(SessionStart.AddMinutes(-10)); // за 10 хв до старту — усередині 30-хвилинного вікна входу

        var handler = new ValidateTicketCommandHandler(db.Context, clock.Object, BuildConfig());

        var result = await handler.Handle(new ValidateTicketCommand(ticket.TicketCode, checkerId), CancellationToken.None);

        result.SeatLabel.Should().Be("Ряд 1, місце 1");
        db.Context.Tickets.Single(t => t.Id == ticket.Id).Status.Should().Be(TicketStatus.USED);
    }

    [Fact]
    public async Task Handle_TooEarly_ThrowsConflict_AndDoesNotChangeStatus()
    {
        using var db = new TestDbContext();
        var ticket = SeedActiveTicket(db.Context, SessionStart, out var checkerId);
        var clock = ClockAt(SessionStart.AddHours(-2)); // задовго до відкриття входу

        var handler = new ValidateTicketCommandHandler(db.Context, clock.Object, BuildConfig());

        var act = async () => await handler.Handle(new ValidateTicketCommand(ticket.TicketCode, checkerId), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>().WithMessage("*Занадто рано*");
        db.Context.Tickets.Single(t => t.Id == ticket.Id).Status.Should().Be(TicketStatus.ACTIVE);
    }

    [Fact]
    public async Task Handle_AfterSessionEnded_ThrowsConflict()
    {
        using var db = new TestDbContext();
        var ticket = SeedActiveTicket(db.Context, SessionStart, out var checkerId);
        var clock = ClockAt(SessionStart.AddHours(5)); // сеанс давно завершився

        var handler = new ValidateTicketCommandHandler(db.Context, clock.Object, BuildConfig());

        var act = async () => await handler.Handle(new ValidateTicketCommand(ticket.TicketCode, checkerId), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>().WithMessage("*завершився*");
    }

    [Fact]
    public async Task Handle_SecondScanOfSameTicket_ThrowsConflict_ProtectsAgainstDoubleEntry()
    {
        using var db = new TestDbContext();
        var ticket = SeedActiveTicket(db.Context, SessionStart, out var checkerId);
        var clock = ClockAt(SessionStart.AddMinutes(-5));
        var handler = new ValidateTicketCommandHandler(db.Context, clock.Object, BuildConfig());

        // Перше сканування — успішне.
        await handler.Handle(new ValidateTicketCommand(ticket.TicketCode, checkerId), CancellationToken.None);

        // Друге сканування ТОГО Ж квитка (напр. показаний іншому контролеру) — має бути відхилене.
        var act = async () => await handler.Handle(new ValidateTicketCommand(ticket.TicketCode, checkerId), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>().WithMessage("*вже використано*");
    }

    [Fact]
    public async Task Handle_UnknownTicketCode_ThrowsNotFound()
    {
        using var db = new TestDbContext();
        var clock = ClockAt(SessionStart);
        var handler = new ValidateTicketCommandHandler(db.Context, clock.Object, BuildConfig());

        var act = async () => await handler.Handle(new ValidateTicketCommand("CV-DOES-NOT-EXIST", Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
