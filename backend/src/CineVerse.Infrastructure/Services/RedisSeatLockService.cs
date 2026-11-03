using CineVerse.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

namespace CineVerse.Infrastructure.Services;

/// <summary>
/// Реалізація атомарного блокування місць через Redis SET key value NX PX ttl (п.2.2 ТЗ:
/// "Redis для реалізації Distributed Lock, що запобігає одночасному бронюванню одного місця
/// двома користувачами"). NX (Not-eXists) гарантує, що лише один запит поставить ключ,
/// якщо кілька прийдуть одночасно — це і є атомарність на рівні однієї Redis-команди.
/// </summary>
public class RedisSeatLockService : ISeatLockService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly TimeSpan _lockDuration;

    public RedisSeatLockService(IConnectionMultiplexer redis, IConfiguration configuration)
    {
        _redis = redis;
        var ttlSeconds = int.TryParse(configuration["SeatLock:TtlSeconds"], out var seconds) ? seconds : 600;
        _lockDuration = TimeSpan.FromSeconds(ttlSeconds);
    }

    public TimeSpan LockDuration => _lockDuration;

    public async Task<bool> TryLockSeatAsync(Guid sessionId, Guid seatId, Guid userId, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var key = BuildKey(sessionId, seatId);

        return await db.StringSetAsync(key, userId.ToString(), _lockDuration, When.NotExists);
    }

    public async Task ReleaseSeatLockAsync(Guid sessionId, Guid seatId, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync(BuildKey(sessionId, seatId));
    }

    public async Task<Guid?> GetLockOwnerAsync(Guid sessionId, Guid seatId, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var value = await db.StringGetAsync(BuildKey(sessionId, seatId));
        return value.HasValue && Guid.TryParse(value.ToString(), out var ownerId) ? ownerId : null;
    }

    private static string BuildKey(Guid sessionId, Guid seatId) => $"seat-lock:{sessionId}:{seatId}";
}
