using ClaimRisk360.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace ClaimRisk360.Services;

/// <summary>
/// Centralized notification service using SignalR.
/// Broadcasts real-time events to all connected clients.
/// </summary>
public class NotificationService
{
    private readonly IHubContext<NotificationHub> _hub;

    public NotificationService(IHubContext<NotificationHub> hub)
    {
        _hub = hub;
    }

    /// <summary>
    /// Send a toast notification to all clients.
    /// </summary>
    public async Task SendNotification(string title, string message, string type = "info", string? claimId = null)
    {
        await _hub.Clients.All.SendAsync("ReceiveNotification", new
        {
            title,
            message,
            type,
            claimId,
            timestamp = DateTime.UtcNow.ToString("HH:mm:ss")
        });
    }

    /// <summary>
    /// Tell all clients to refresh their current page data.
    /// </summary>
    public async Task SendDataRefresh(string area, string? entityId = null)
    {
        await _hub.Clients.All.SendAsync("DataRefresh", new
        {
            area,
            entityId,
            timestamp = DateTime.UtcNow.ToString("HH:mm:ss")
        });
    }

    /// <summary>
    /// Update the notification badge count on all clients.
    /// </summary>
    public async Task SendBadgeUpdate(int pendingCount)
    {
        await _hub.Clients.All.SendAsync("BadgeUpdate", pendingCount);
    }
}
