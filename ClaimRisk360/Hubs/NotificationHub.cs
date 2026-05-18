using Microsoft.AspNetCore.SignalR;

namespace ClaimRisk360.Hubs;

/// <summary>
/// SignalR hub for real-time notifications across all connected clients.
/// </summary>
public class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }
}
