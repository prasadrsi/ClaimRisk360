using ClaimRisk360.Api.Models;
using ClaimRisk360.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClaimRisk360.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClaimRulesController : ControllerBase
{
    private readonly ClaimRuleEvaluationService _ruleService;
    private readonly AzureFoundryAgentService _agentService;
    private readonly ClaimReviewNotifier _notifier;

    public ClaimRulesController(ClaimRuleEvaluationService ruleService, AzureFoundryAgentService agentService, ClaimReviewNotifier notifier)
    {
        _ruleService = ruleService;
        _agentService = agentService;
        _notifier = notifier;
    }

    /// <summary>
    /// Evaluate claim rules for fraud detection. Results are broadcast in real-time via SignalR.
    /// </summary>
    [HttpPost("evaluate")]
    [ProducesResponseType(typeof(ClaimRuleEvaluationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> EvaluateRules([FromBody] ClaimRuleEvaluationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ClaimId))
            return BadRequest(new { error = "ClaimId is required" });

        var response = _ruleService.Evaluate(request);

        // Get AI agent analysis
        response.AgentAnalysis = await _agentService.AnalyzeClaimRulesAsync(request, response.Violations);

        // Broadcast real-time notification
        await _notifier.NotifyRuleEvaluationAsync(response);

        return Ok(response);
    }
}
