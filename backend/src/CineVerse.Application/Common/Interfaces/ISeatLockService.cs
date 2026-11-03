namespace CineVerse.Application.Common.Interfaces;

/// <summary>
/// Сервіс атомарного тимчасового блокування місць у залі через Redis (Distributed Lock),
/// щоб уникнути Race Condition при паралельному бронюванні одного й того ж місця.
/// </summary>
public interface ISeatLockService
{
    /// <summary>Тривалість блокування місця = тривалість дії бронювання PENDING (за замовчуванням 10 хв),
    /// узгоджена між Redis TTL і Booking.ExpiresAt, щоб обидва механізми "згасали" одночасно.</summary>
    TimeSpan LockDuration { get; }

    /// <summary>Спроба атомарно заблокувати місце на сеанс. TTL = LockDuration.</summary>
    Task<bool> TryLockSeatAsync(Guid sessionId, Guid seatId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Зняти блокування (напр. після успішної оплати місце переходить у статус SOLD в БД).</summary>
    Task ReleaseSeatLockAsync(Guid sessionId, Guid seatId, CancellationToken cancellationToken = default);

    /// <summary>Перевірити, чи заблоковане місце (і ким).</summary>
    Task<Guid?> GetLockOwnerAsync(Guid sessionId, Guid seatId, CancellationToken cancellationToken = default);
}
