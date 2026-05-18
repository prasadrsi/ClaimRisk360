using ClaimRisk360.Api.Models;
using ClaimRisk360.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClaimRisk360.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentValidationController : ControllerBase
{
    private readonly DocumentValidationService _documentService;
    private readonly AzureFoundryAgentService _agentService;
    private readonly ClaimReviewNotifier _notifier;

    public DocumentValidationController(DocumentValidationService documentService, AzureFoundryAgentService agentService, ClaimReviewNotifier notifier)
    {
        _documentService = documentService;
        _agentService = agentService;
        _notifier = notifier;
    }

    /// <summary>
    /// Validate a claim document for completeness and integrity. Results are broadcast in real-time via SignalR.
    /// </summary>
    [HttpPost("validate")]
    [ProducesResponseType(typeof(DocumentValidationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ValidateDocument([FromBody] DocumentValidationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.DocumentId))
            return BadRequest(new { error = "DocumentId is required" });

        var response = _documentService.Validate(request);

        // Get AI agent analysis
        response.AgentAnalysis = await _agentService.AnalyzeDocumentAsync(request, response.Issues);

        // Broadcast real-time notification
        await _notifier.NotifyDocumentValidationAsync(response);

        return Ok(response);
    }
}
