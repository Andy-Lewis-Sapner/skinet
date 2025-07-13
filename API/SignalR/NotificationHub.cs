using System.Collections.Concurrent;
using API.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace API.SignalR;

[Authorize]
public class NotificationHub : Hub
{
    private static readonly ConcurrentDictionary<string, string> UserConnections = new();

    public override Task OnConnectedAsync()
    {
        string? email = Context.User?.GetEmail();
        if (!string.IsNullOrEmpty(email)) UserConnections[email] = Context.ConnectionId;
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        string? email = Context.User?.GetEmail();
        if (!string.IsNullOrEmpty(email)) UserConnections.TryRemove(email, out _);
        return base.OnDisconnectedAsync(exception);
    }

    public static string? GetConnectionIdByEmail(string email)
    {
        UserConnections.TryGetValue(email, out string? connectionId);
        return connectionId;
    }
}
