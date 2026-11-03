using CineVerse.Application.Common.Exceptions;
using CineVerse.Application.Common.Interfaces;
using CineVerse.Application.Features.Bookings.Common;
using CineVerse.Domain.Entities;
using CineVerse.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineVerse.Application.Features.Bookings.Commands.CreateBooking;

public class CreateBookingCommandHandler : IRequestHandler<CreateBookingCommand, BookingDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ISeatLockService _seatLockService;
    private readonly ISeatStatusNotifier _seatStatusNotifier;
    private readonly IDateTime _dateTime;

    public CreateBookingCommandHandler(
        IApplicationDbContext context,
        ISeatLockService seatLockService,
        ISeatStatusNotifier seatStatusNotifier,
        IDateTime dateTime)
    {
        _context = context;
        _seatLockService = seatLockService;
        _seatStatusNotifier = seatStatusNotifier;
        _dateTime = dateTime;
    }

    public async Task<BookingDto> Handle(CreateBookingCommand request, CancellationToken cancellationToken)
    {
        var session = await _context.Sessions
            .Include(s => s.Movie)
            .Include(s => s.Hall).ThenInclude(h => h.Cinema)
            .FirstOrDefaultAsync(s => s.Id == request.SessionId, cancellationToken)
            ?? throw new NotFoundException(nameof(Session), request.SessionId);

        if (session.StartTime <= _dateTime.UtcNow)
        {
            throw new ConflictException("Неможливо забронювати місця на сеанс, який уже почався або завершився.");
        }

        var seats = await _context.Seats
            .Where(s => request.SeatIds.Contains(s.Id) && s.HallId == session.HallId)
            .ToListAsync(cancellationToken);

        if (seats.Count != request.SeatIds.Count)
        {
            throw new NotFoundException("Одне або декілька обраних місць не належать залу цього сеансу.");
        }

        // --- Крок 1: атомарне блокування кожного місця в Redis (Distributed Lock) ---
        // Це і є захист від Race Condition: якщо два користувачі одночасно тиснуть "забронювати"
        // на те саме місце, лише один SET NX встигне спрацювати успішно — другий отримає false
        // ще ДО будь-якого звернення до PostgreSQL, без потреби в довгих транзакціях чи SELECT FOR UPDATE.
        var acquiredLocks = new List<Guid>();
        var conflictingSeatIds = new List<Guid>();

        foreach (var seatId in request.SeatIds)
        {
            var locked = await _seatLockService.TryLockSeatAsync(request.SessionId, seatId, request.UserId, cancellationToken);
            if (locked)
            {
                acquiredLocks.Add(seatId);
            }
            else
            {
                conflictingSeatIds.Add(seatId);
            }
        }

        if (conflictingSeatIds.Count > 0)
        {
            await ReleaseAcquiredLocksAsync(acquiredLocks, request.SessionId, cancellationToken);
            throw new ConflictException(
                $"Місця вже обираються іншим користувачем: {string.Join(", ", conflictingSeatIds)}. Оберіть інші місця.");
        }

        try
        {
            // --- Крок 2: контрольна перевірка в БД ---
            // Redis-лок унеможливлює одночасне проходження двох запитів далі цього рядка для ОДНОГО
            // й того ж місця, але про всяк випадок (напр. лишок від попереднього PENDING, що ще не
            // протермінувався за годинником БД) додатково звіряємось із джерелом істини — PostgreSQL.
            var now = _dateTime.UtcNow;
            var alreadyOccupied = await _context.BookingSeats
                .Include(bs => bs.Booking)
                .Where(bs => bs.Booking.SessionId == request.SessionId && request.SeatIds.Contains(bs.SeatId))
                .Where(bs => bs.Booking.Status == BookingStatus.CONFIRMED ||
                            (bs.Booking.Status == BookingStatus.PENDING && bs.Booking.ExpiresAt > now))
                .Select(bs => bs.SeatId)
                .ToListAsync(cancellationToken);

            if (alreadyOccupied.Count > 0)
            {
                throw new ConflictException($"Місця вже заброньовано: {string.Join(", ", alreadyOccupied)}.");
            }

            var lockDuration = _seatLockService.LockDuration;
            var expiresAt = now.Add(lockDuration);

            var booking = new Booking
            {
                UserId = request.UserId,
                SessionId = request.SessionId,
                Status = BookingStatus.PENDING,
                ExpiresAt = expiresAt
            };

            decimal total = 0;
            foreach (var seat in seats)
            {
                var price = SeatPricingCalculator.CalculatePrice(session.BasePrice, seat.Type);
                total += price;
                booking.BookingSeats.Add(new BookingSeat { BookingId = booking.Id, SeatId = seat.Id, Price = price });
            }

            // --- Промокод (п.2.1 ТЗ, сценарій "STUDENT20") ---
            // Знижка рахується від ЗАГАЛЬНОЇ суми бронювання, а не від ціни кожного місця окремо —
            // так простіше й зрозуміліше показати клієнту "було/стало". UsageCount інкрементується
            // ТУТ, у тій самій транзакції, що й саме бронювання: якщо збереження впаде — відкотиться
            // і лічильник використань, промокод не "згорить" даремно.
            Promotion? promotion = null;
            decimal discountAmount = 0m;

            if (!string.IsNullOrWhiteSpace(request.PromoCode))
            {
                var normalizedCode = request.PromoCode.Trim().ToUpperInvariant();
                promotion = await _context.Promotions
                    .FirstOrDefaultAsync(p => p.Code.ToUpper() == normalizedCode, cancellationToken);

                if (promotion is null)
                {
                    throw new ConflictException($"Промокод \"{request.PromoCode}\" не знайдено.");
                }
                if (promotion.ExpiresAt <= now)
                {
                    throw new ConflictException($"Термін дії промокоду \"{promotion.Code}\" минув.");
                }
                if (promotion.UsageLimit.HasValue && promotion.UsageCount >= promotion.UsageLimit.Value)
                {
                    throw new ConflictException($"Промокод \"{promotion.Code}\" вичерпав ліміт використань.");
                }

                discountAmount = promotion.DiscountType == DiscountType.PERCENT
                    ? Math.Round(total * (promotion.DiscountValue / 100m), 2)
                    : Math.Min(promotion.DiscountValue, total); // фіксована знижка не робить суму від'ємною

                promotion.UsageCount += 1;
            }

            booking.TotalPrice = total - discountAmount;
            booking.PromotionId = promotion?.Id;
            booking.DiscountAmount = discountAmount;

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync(cancellationToken);

            // --- Крок 3: сповіщення в реальному часі всіх, хто зараз дивиться на цю схему залу ---
            await _seatStatusNotifier.NotifySeatsUpdatedAsync(
                request.SessionId,
                request.SeatIds.Select(id => new SeatStatusChange(id, "LOCKED")),
                cancellationToken);

            return new BookingDto(
                booking.Id, booking.SessionId, booking.Status.ToString(), booking.TotalPrice, booking.ExpiresAt,
                seats.Select(s => new BookingSeatResultDto(
                    s.Id, s.RowNumber, s.SeatNumber,
                    booking.BookingSeats.First(bs => bs.SeatId == s.Id).Price)).ToList(),
                session.Movie.Title, session.Movie.PosterUrl, session.Hall.Name, session.Hall.Cinema.Name, session.StartTime,
                promotion?.Code, discountAmount
            );
        }
        catch
        {
            // Будь-яка помилка після захоплення Redis-локів (включно з ConflictException з БД-перевірки
            // чи збоєм SaveChangesAsync) — локи звільняються, щоб не "заморозити" місця даремно.
            await ReleaseAcquiredLocksAsync(acquiredLocks, request.SessionId, cancellationToken);
            throw;
        }
    }

    private async Task ReleaseAcquiredLocksAsync(List<Guid> seatIds, Guid sessionId, CancellationToken cancellationToken)
    {
        foreach (var seatId in seatIds)
        {
            await _seatLockService.ReleaseSeatLockAsync(sessionId, seatId, cancellationToken);
        }
    }
}
