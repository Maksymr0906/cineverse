namespace CineVerse.Application.Common.Interfaces;

public record SeatStatusChange(Guid SeatId, string Status); // "FREE" | "LOCKED" | "SOLD"

/// <summary>
/// Абстракція над каналом реального часу (SignalR), яким Application-шар сповіщає всіх
/// клієнтів, що дивляться на схему конкретного сеансу, про зміну статусу місць.
/// Реалізація живе у WebApi (там-таки, де IHubContext&lt;HallHub&gt;), щоб Application
/// не залежав від ASP.NET Core SignalR напряму.
/// </summary>
public interface ISeatStatusNotifier
{
    Task NotifySeatsUpdatedAsync(Guid sessionId, IEnumerable<SeatStatusChange> changes, CancellationToken cancellationToken = default);
}
