using ClaimRisk360.Api.Models;
using ClaimRisk360.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClaimRisk360.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClaimValidationController : ControllerBase
{
    private readonly ClaimValidationService _validationService;
    private readonly AzureFoundryAgentService _agentService;
    private readonly ClaimReviewNotifier _notifier;

    private readonly ClaimRuleEvaluationService _ruleService;

    public ClaimValidationController(ClaimValidationService validationService, AzureFoundryAgentService agentService, ClaimReviewNotifier notifier, ClaimRuleEvaluationService ruleService)
    {
        _validationService = validationService;
        _agentService = agentService;
        _notifier = notifier;
        _ruleService = ruleService;
    }

    /// <summary>
    /// Validate claim data fields and business rules. Results are broadcast in real-time via SignalR.
    /// </summary>
    [HttpPost("validate")]
    [ProducesResponseType(typeof(ClaimValidationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ValidateClaim([FromBody] ClaimValidationRequest request)
    {
        var response = _validationService.Validate(request);

        // Run rule evaluation to assess risk even during validation
        var ruleRequest = new ClaimRuleEvaluationRequest
        {
            ClaimId = $"CLM-{DateTime.UtcNow:yyyyMMddHHmmss}",
            PatientName = request.PatientName,
            PatientId = request.PatientId,
            ProviderName = request.ProviderName,
            ProviderId = request.ProviderId,
            Specialty = request.Specialty,
            DiagnosisCode = request.DiagnosisCode,
            ProcedureCode = request.ProcedureCode,
            Amount = request.Amount,
            ServiceDate = request.ServiceDate,
            Location = request.Location
        };

        var ruleResult = _ruleService.Evaluate(ruleRequest);
        response.RiskScore = ruleResult.RiskScore;
        response.RiskCategory = ruleResult.RiskCategory;
        response.RiskViolations = ruleResult.Violations;
        response.FeatureContributions = ruleResult.FeatureContributions;

        // Call Azure Foundry Agent for AI-powered validation analysis
        response.AgentAnalysis = await _agentService.AnalyzeClaimValidationAsync(request, response);

        // Broadcast real-time notification
        await _notifier.NotifyValidationAsync(ruleRequest.ClaimId, response);

        return Ok(response);
    }
}

