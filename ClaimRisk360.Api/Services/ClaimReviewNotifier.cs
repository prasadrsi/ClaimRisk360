using ClaimRisk360.Api.Hubs;
using ClaimRisk360.Api.Models;
using Microsoft.AspNetCore.SignalR;

namespace ClaimRisk360.Api.Services;

/// <summary>
/// Service that broadcasts real-time claim review events via SignalR.
/// </summary>
public class ClaimReviewNotifier
{
    private readonly IHubContext<ClaimReviewHub> _hubContext;
    private readonly ILogger<ClaimReviewNotifier> _logger;

    public ClaimReviewNotifier(IHubContext<ClaimReviewHub> hubContext, ILogger<ClaimReviewNotifier> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <summary>
    /// Broadcast a rule evaluation result in real-time.
    /// </summary>
    public async Task NotifyRuleEvaluationAsync(ClaimRuleEvaluationResponse result)
    {
        var payload = new
        {
            EventType = "RuleEvaluation",
            result.ClaimId,
            result.HasViolations,
            result.RiskScore,
            result.RiskCategory,
            ViolationCount = result.Violations.Count,
            result.AgentAnalysis,
            Timestamp = DateTime.UtcNow
        };

        await _hubContext.Clients.Group($"claim-{result.ClaimId}").SendAsync("ClaimReviewed", payload);
        await _hubContext.Clients.Group("all-reviews").SendAsync("ClaimReviewed", payload);

        _logger.LogInformation("Real-time notification sent for claim {ClaimId} (Risk: {RiskCategory})", result.ClaimId, result.RiskCategory);
    }

    /// <summary>
    /// Broadcast a claim validation result in real-time.
    /// </summary>
    public async Task NotifyValidationAsync(string claimId, ClaimValidationResponse result)
    {
        var payload = new
        {
            EventType = "Validation",
            ClaimId = claimId,
            result.IsValid,
            result.Status,
            ErrorCount = result.Errors.Count,
            result.Warnings,
            Timestamp = DateTime.UtcNow
        };

        await _hubContext.Clients.Group($"claim-{claimId}").SendAsync("ClaimValidated", payload);
        await _hubContext.Clients.Group("all-reviews").SendAsync("ClaimValidated", payload);
    }

    /// <summary>
    /// Broadcast a document validation result in real-time.
    /// </summary>
    public async Task NotifyDocumentValidationAsync(DocumentValidationResponse result)
    {
        var payload = new
        {
            EventType = "DocumentValidation",
            result.DocumentId,
            result.ClaimId,
            result.IsValid,
            result.Status,
            IssueCount = result.Issues.Count,
            result.AgentAnalysis,
            Timestamp = DateTime.UtcNow
        };

        await _hubContext.Clients.Group($"claim-{result.ClaimId}").SendAsync("DocumentValidated", payload);
        await _hubContext.Clients.Group("all-reviews").SendAsync("DocumentValidated", payload);
    }
}
