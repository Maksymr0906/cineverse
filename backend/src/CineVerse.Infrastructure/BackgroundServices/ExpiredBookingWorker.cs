using CineVerse.Application.Common.Interfaces;
using CineVerse.Domain.Enums;
using CineVerse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CineVerse.Infrastructure.BackgroundServices;

/// <summary>
/// Фоновий воркер (п.2.2 ТЗ: "автоматичне скасування бронювання після 10 хв неоплати").
/// PostgreSQL не має вбудованого TTL на рядки, тому цей сервіс періодично сканує PENDING-
/// бронювання, чий ExpiresAt настав, переводить їх у EXPIRED, явно звільняє відповідні
/// Redis-локи (подвійна гарантія — навіть якщо TTL у Redis чомусь розійшовся з БД)
/// і сповіщає через SignalR усіх, хто дивиться на цю схему залу, що місця знову вільні.
/// </summary>
public class ExpiredBookingWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExpiredBookingWorker> _logger;
    private readonly TimeSpan _pollInterval;

    public ExpiredBookingWorker(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<ExpiredBookingWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        var seconds = int.TryParse(configuration["BookingExpiry:PollIntervalSeconds"], out var s) ? s : 15;
        _pollInterval = TimeSpan.FromSeconds(seconds);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ExpiredBookingWorker запущено, інтервал перевірки: {Interval}", _pollInterval);

        using var timer = new PeriodicTimer(_pollInterval);
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ProcessExpiredBookingsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка під час обробки протермінованих бронювань.");
            }
        }
    }

    private async Task ProcessExpiredBookingsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var seatLockService = scope.ServiceProvider.GetRequiredService<ISeatLockService>();
        var seatStatusNotifier = scope.ServiceProvider.GetRequiredService<ISeatStatusNotifier>();
        var dateTime = scope.ServiceProvider.GetRequiredService<IDateTime>();

        var now = dateTime.UtcNow;

        var expiredBookings = await context.Bookings
            .Include(b => b.BookingSeats)
            .Where(b => b.Status == BookingStatus.PENDING && b.ExpiresAt <= now)
            .ToListAsync(cancellationToken);

        if (expiredBookings.Count == 0) return;

        _logger.LogInformation("Знайдено {Count} протермінованих бронювань — скасовуємо.", expiredBookings.Count);

        foreach (var booking in expiredBookings)
        {
            booking.Status = BookingStatus.EXPIRED;
        }

        await context.SaveChangesAsync(cancellationToken);

        foreach (var booking in expiredBookings)
        {
            foreach (var bookingSeat in booking.BookingSeats)
            {
                await seatLockService.ReleaseSeatLockAsync(booking.SessionId, bookingSeat.SeatId, cancellationToken);
            }

            await seatStatusNotifier.NotifySeatsUpdatedAsync(
                booking.SessionId,
                booking.BookingSeats.Select(bs => new SeatStatusChange(bs.SeatId, "FREE")),
                cancellationToken);
        }
    }
}
