using ClaimRisk360.Api.Models;
using Microsoft.AspNetCore.SignalR;

namespace ClaimRisk360.Api.Hubs;

/// <summary>
/// SignalR hub for real-time claim review notifications.
/// Clients can subscribe to specific claim updates or receive all evaluations in real-time.
/// </summary>
public class ClaimReviewHub : Hub
{
    /// <summary>
    /// Join a group to receive updates for a specific claim.
    /// </summary>
    public async Task SubscribeToClaim(string claimId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"claim-{claimId}");
        await Clients.Caller.SendAsync("Subscribed", new { claimId, message = $"Subscribed to real-time updates for claim {claimId}" });
    }

    /// <summary>
    /// Leave a claim-specific group.
    /// </summary>
    public async Task UnsubscribeFromClaim(string claimId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"claim-{claimId}");
    }

    /// <summary>
    /// Join the global feed to receive all claim review events.
    /// </summary>
    public async Task SubscribeToAllReviews()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "all-reviews");
        await Clients.Caller.SendAsync("Subscribed", new { message = "Subscribed to all claim review events" });
    }
}
