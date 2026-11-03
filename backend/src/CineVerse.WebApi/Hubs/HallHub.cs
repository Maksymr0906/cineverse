using Microsoft.AspNetCore.SignalR;

namespace CineVerse.WebApi.Hubs;

/// <summary>
/// SignalR-хаб для реального часу: коли один клієнт обирає/звільняє місце,
/// усі інші клієнти на сторінці цього сеансу миттєво бачать зміну статусу.
/// Групи іменуються за SessionId, щоб оновлення надходили лише глядачам конкретного сеансу.
/// </summary>
public class HallHub : Hub
{
    public async Task JoinSessionGroup(string sessionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(sessionId));
    }

    public async Task LeaveSessionGroup(string sessionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(sessionId));
    }

    public static string GroupName(string sessionId) => $"session-{sessionId}";
}
