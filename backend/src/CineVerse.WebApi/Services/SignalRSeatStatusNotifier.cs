using CineVerse.Application.Common.Interfaces;
using CineVerse.WebApi.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace CineVerse.WebApi.Services;

/// <summary>
/// Реалізація ISeatStatusNotifier поверх SignalR: розсилає подію "seatsUpdated" усім клієнтам,
/// що приєднались до групи конкретного сеансу (HallHub.JoinSessionGroup на фронтенді),
/// щоб схема залу оновлювалась у всіх переглядачів миттєво, без polling.
/// </summary>
public class SignalRSeatStatusNotifier : ISeatStatusNotifier
{
    private readonly IHubContext<HallHub> _hubContext;

    public SignalRSeatStatusNotifier(IHubContext<HallHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifySeatsUpdatedAsync(Guid sessionId, IEnumerable<SeatStatusChange> changes, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients
            .Group(HallHub.GroupName(sessionId.ToString()))
            .SendAsync("seatsUpdated", changes.Select(c => new { seatId = c.SeatId, status = c.Status }), cancellationToken);
    }
}
