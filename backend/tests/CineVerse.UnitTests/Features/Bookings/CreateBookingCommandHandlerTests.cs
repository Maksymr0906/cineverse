using CineVerse.Application.Common.Exceptions;
using CineVerse.Application.Common.Interfaces;
using CineVerse.Application.Features.Bookings.Commands.CreateBooking;
using CineVerse.Domain.Entities;
using CineVerse.Domain.Enums;
using CineVerse.UnitTests.Common;
using FluentAssertions;
using Moq;
using Xunit;

namespace CineVerse.UnitTests.Features.Bookings;

/// <summary>
/// Тести ядра Етапу 5 — атомарного бронювання з Redis-локом. ISeatLockService/ISeatStatusNotifier/
/// IDateTime замоковано (Moq), а PostgreSQL замінено на SQLite in-memory (TestDbContext) —
/// це дозволяє перевірити РЕАЛЬНУ логіку хендлера (включно зі збереженням у БД), не піднімаючи Docker.
/// </summary>
public class CreateBookingCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 10, 12, 0, 0, TimeSpan.Zero);

    private static (Mock<ISeatLockService> LockService, Mock<ISeatStatusNotifier> Notifier, Mock<IDateTime> Clock) CreateMocks(TimeSpan? lockDuration = null)
    {
        var lockService = new Mock<ISeatLockService>();
        lockService.SetupGet(x => x.LockDuration).Returns(lockDuration ?? TimeSpan.FromMinutes(10));

        var notifier = new Mock<ISeatStatusNotifier>();

        var clock = new Mock<IDateTime>();
        clock.SetupGet(x => x.UtcNow).Returns(Now);

        return (lockService, notifier, clock);
    }

    [Fact]
    public async Task Handle_AllSeatsAvailable_CreatesBookingAndNotifiesLocked()
    {
        using var db = new TestDbContext();
        var (session, seats) = TestDataBuilder.SeedSessionWithSeats(db.Context, Now.AddHours(2), basePrice: 150m);
        var user = TestDataBuilder.SeedUser(db.Context);
        var seatIds = seats.Take(2).Select(s => s.Id).ToList();

        var (lockService, notifier, clock) = CreateMocks();
        lockService.Setup(x => x.TryLockSeatAsync(session.Id, It.IsAny<Guid>(), user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new CreateBookingCommandHandler(db.Context, lockService.Object, notifier.Object, clock.Object);

        var result = await handler.Handle(new CreateBookingCommand(user.Id, session.Id, seatIds), CancellationToken.None);

        result.Status.Should().Be("PENDING");
        result.TotalPrice.Should().Be(300m); // 2 місця STANDARD по 150 грн
        result.ExpiresAt.Should().Be(Now.AddMinutes(10));
        db.Context.Bookings.Should().ContainSingle(b => b.Id == result.Id);

        notifier.Verify(x => x.NotifySeatsUpdatedAsync(
            session.Id,
            It.Is<IEnumerable<SeatStatusChange>>(changes => changes.Count() == 2 && changes.All(c => c.Status == "LOCKED")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_SecondSeatLockFails_ThrowsConflict_AndReleasesFirstAcquiredLock()
    {
        using var db = new TestDbContext();
        var (session, seats) = TestDataBuilder.SeedSessionWithSeats(db.Context, Now.AddHours(2));
        var user = TestDataBuilder.SeedUser(db.Context);
        var winningSeatId = seats[0].Id;
        var contestedSeatId = seats[1].Id;

        var (lockService, notifier, clock) = CreateMocks();
        lockService.Setup(x => x.TryLockSeatAsync(session.Id, winningSeatId, user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        lockService.Setup(x => x.TryLockSeatAsync(session.Id, contestedSeatId, user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var handler = new CreateBookingCommandHandler(db.Context, lockService.Object, notifier.Object, clock.Object);
        var command = new CreateBookingCommand(user.Id, session.Id, new[] { winningSeatId, contestedSeatId });

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();

        // Ключова перевірка Race-Condition-захисту: місце, лок на яке МИ встигли взяти,
        // має бути звільнене, щойно з'ясувалось, що бронювання в цілому не відбудеться.
        lockService.Verify(x => x.ReleaseSeatLockAsync(session.Id, winningSeatId, It.IsAny<CancellationToken>()), Times.Once);
        lockService.Verify(x => x.ReleaseSeatLockAsync(session.Id, contestedSeatId, It.IsAny<CancellationToken>()), Times.Never);

        db.Context.Bookings.Should().BeEmpty("невдале бронювання не повинно лишати часткові записи в БД");
    }

    [Fact]
    public async Task Handle_SessionAlreadyStarted_ThrowsConflict_WithoutAttemptingAnyLock()
    {
        using var db = new TestDbContext();
        var (session, seats) = TestDataBuilder.SeedSessionWithSeats(db.Context, Now.AddMinutes(-30)); // сеанс уже почався
        var user = TestDataBuilder.SeedUser(db.Context);

        var (lockService, notifier, clock) = CreateMocks();
        var handler = new CreateBookingCommandHandler(db.Context, lockService.Object, notifier.Object, clock.Object);

        var act = async () => await handler.Handle(new CreateBookingCommand(user.Id, session.Id, new[] { seats[0].Id }), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        lockService.Verify(x => x.TryLockSeatAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SeatFromDifferentHall_ThrowsNotFound()
    {
        using var db = new TestDbContext();
        var (session, _) = TestDataBuilder.SeedSessionWithSeats(db.Context, Now.AddHours(2));
        var (otherSession, otherSeats) = TestDataBuilder.SeedSessionWithSeats(db.Context, Now.AddHours(3));
        var user = TestDataBuilder.SeedUser(db.Context);

        var (lockService, notifier, clock) = CreateMocks();
        var handler = new CreateBookingCommandHandler(db.Context, lockService.Object, notifier.Object, clock.Object);

        // Місце належить залу ІНШОГО сеансу — має бути відхилено ще до спроби локу.
        var act = async () => await handler.Handle(new CreateBookingCommand(user.Id, session.Id, new[] { otherSeats[0].Id }), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // --- Промокоди (доопрацьовано за результатами аудиту, сценарій ТЗ "STUDENT20") ---

    private static Promotion SeedPromotion(
        CineVerse.Infrastructure.Persistence.ApplicationDbContext context,
        string code, DiscountType type, decimal value, DateTimeOffset expiresAt, int? usageLimit = null, int usageCount = 0)
    {
        var promo = new Promotion { Code = code, DiscountType = type, DiscountValue = value, ExpiresAt = expiresAt, UsageLimit = usageLimit, UsageCount = usageCount };
        context.Promotions.Add(promo);
        context.SaveChanges();
        return promo;
    }

    [Fact]
    public async Task Handle_ValidPercentPromoCode_AppliesDiscountAndIncrementsUsage()
    {
        using var db = new TestDbContext();
        var (session, seats) = TestDataBuilder.SeedSessionWithSeats(db.Context, Now.AddHours(2), basePrice: 200m);
        var user = TestDataBuilder.SeedUser(db.Context);
        SeedPromotion(db.Context, "STUDENT20", DiscountType.PERCENT, 20m, Now.AddDays(30));

        var (lockService, notifier, clock) = CreateMocks();
        lockService.Setup(x => x.TryLockSeatAsync(session.Id, It.IsAny<Guid>(), user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var handler = new CreateBookingCommandHandler(db.Context, lockService.Object, notifier.Object, clock.Object);

        var result = await handler.Handle(
            new CreateBookingCommand(user.Id, session.Id, new[] { seats[0].Id }, "student20"), // навмисно нижній регістр
            CancellationToken.None);

        result.TotalPrice.Should().Be(160m); // 200 - 20%
        result.DiscountAmount.Should().Be(40m);
        result.AppliedPromoCode.Should().Be("STUDENT20");

        db.Context.Promotions.Single(p => p.Code == "STUDENT20").UsageCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_FixedAmountPromoCode_NeverMakesTotalNegative()
    {
        using var db = new TestDbContext();
        var (session, seats) = TestDataBuilder.SeedSessionWithSeats(db.Context, Now.AddHours(2), basePrice: 30m);
        var user = TestDataBuilder.SeedUser(db.Context);
        // Фіксована знижка (100 грн) БІЛЬША за суму бронювання (30 грн) — сума не має піти в мінус.
        SeedPromotion(db.Context, "WELCOME100", DiscountType.FIXED, 100m, Now.AddDays(30));

        var (lockService, notifier, clock) = CreateMocks();
        lockService.Setup(x => x.TryLockSeatAsync(session.Id, It.IsAny<Guid>(), user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var handler = new CreateBookingCommandHandler(db.Context, lockService.Object, notifier.Object, clock.Object);

        var result = await handler.Handle(new CreateBookingCommand(user.Id, session.Id, new[] { seats[0].Id }, "WELCOME100"), CancellationToken.None);

        result.TotalPrice.Should().Be(0m);
        result.DiscountAmount.Should().Be(30m);
    }

    [Fact]
    public async Task Handle_ExpiredPromoCode_ThrowsConflict_AndReleasesLocks()
    {
        using var db = new TestDbContext();
        var (session, seats) = TestDataBuilder.SeedSessionWithSeats(db.Context, Now.AddHours(2));
        var user = TestDataBuilder.SeedUser(db.Context);
        SeedPromotion(db.Context, "EXPIRED10", DiscountType.PERCENT, 10m, Now.AddDays(-1)); // вже протермінований

        var (lockService, notifier, clock) = CreateMocks();
        lockService.Setup(x => x.TryLockSeatAsync(session.Id, It.IsAny<Guid>(), user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var handler = new CreateBookingCommandHandler(db.Context, lockService.Object, notifier.Object, clock.Object);

        var act = async () => await handler.Handle(new CreateBookingCommand(user.Id, session.Id, new[] { seats[0].Id }, "EXPIRED10"), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>().WithMessage("*минув*");
        lockService.Verify(x => x.ReleaseSeatLockAsync(session.Id, seats[0].Id, It.IsAny<CancellationToken>()), Times.Once);
        db.Context.Bookings.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ExhaustedPromoCode_ThrowsConflict()
    {
        using var db = new TestDbContext();
        var (session, seats) = TestDataBuilder.SeedSessionWithSeats(db.Context, Now.AddHours(2));
        var user = TestDataBuilder.SeedUser(db.Context);
        SeedPromotion(db.Context, "LIMITED1", DiscountType.PERCENT, 10m, Now.AddDays(30), usageLimit: 1, usageCount: 1); // ліміт вичерпано

        var (lockService, notifier, clock) = CreateMocks();
        lockService.Setup(x => x.TryLockSeatAsync(session.Id, It.IsAny<Guid>(), user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var handler = new CreateBookingCommandHandler(db.Context, lockService.Object, notifier.Object, clock.Object);

        var act = async () => await handler.Handle(new CreateBookingCommand(user.Id, session.Id, new[] { seats[0].Id }, "LIMITED1"), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>().WithMessage("*ліміт*");
    }

    [Fact]
    public async Task Handle_UnknownPromoCode_ThrowsConflict()
    {
        using var db = new TestDbContext();
        var (session, seats) = TestDataBuilder.SeedSessionWithSeats(db.Context, Now.AddHours(2));
        var user = TestDataBuilder.SeedUser(db.Context);

        var (lockService, notifier, clock) = CreateMocks();
        lockService.Setup(x => x.TryLockSeatAsync(session.Id, It.IsAny<Guid>(), user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var handler = new CreateBookingCommandHandler(db.Context, lockService.Object, notifier.Object, clock.Object);

        var act = async () => await handler.Handle(new CreateBookingCommand(user.Id, session.Id, new[] { seats[0].Id }, "DOES-NOT-EXIST"), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>().WithMessage("*не знайдено*");
    }
}
