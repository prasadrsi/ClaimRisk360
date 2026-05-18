using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using Azure.AI.Extensions.OpenAI;
using Azure.Identity;
using OpenAI.Responses;
using ClaimRisk360.Api.Models;

#pragma warning disable OPENAI001

namespace ClaimRisk360.Api.Services;

/// <summary>
/// Service that integrates with Azure Foundry Agent for AI-powered claim analysis.
/// Uses Azure.AI.Projects SDK with ProjectResponsesClient for agent interactions.
/// </summary>
public class AzureFoundryAgentService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AzureFoundryAgentService> _logger;

    private const string LogDirectory = "Logs";
    private const string LogFileName = "AzureFoundryAgent.log";

    public AzureFoundryAgentService(IConfiguration configuration, ILogger<AzureFoundryAgentService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        // Ensure log directory exists
        if (!Directory.Exists(LogDirectory))
            Directory.CreateDirectory(LogDirectory);
    }

    public async Task<string> AnalyzeClaimRulesAsync(ClaimRuleEvaluationRequest request, List<RuleViolation> violations)
    {
        try
        {
            var prompt = $"""
                Analyze the following claim for potential fraud risk:
                - Claim ID: {request.ClaimId}
                - Patient: {request.PatientName} ({request.PatientId})
                - Provider: {request.ProviderName} ({request.ProviderId})
                - Diagnosis: {request.DiagnosisCode}, Procedure: {request.ProcedureCode}
                - Amount: ${request.Amount:N2}
                - Service Date: {request.ServiceDate:yyyy-MM-dd}
                - Location: {request.Location}
                - Rule Violations Found: {violations.Count}
                {string.Join("\n", violations.Select(v => $"  - {v.RuleName} ({v.Severity}): {v.Description}"))}

                Provide a brief risk assessment and recommendation.
                """;

            return await CallAgentAsync(prompt, "ClaimRuleAnalysis");
        }
        catch (Exception ex)
        {
            LogToFile($"ERROR [AnalyzeClaimRulesAsync]: {ex}");
            _logger.LogError(ex, "Error calling Azure Foundry agent for claim rule analysis");
            return string.Empty;
        }
    }

    public async Task<string> AnalyzeClaimValidationAsync(ClaimValidationRequest request, ClaimValidationResponse validationResponse)
    {
        try
        {
            var prompt = $"""
                Analyze the following claim validation results and provide risk assessment for approval:
                - Patient: {request.PatientName} ({request.PatientId})
                - Provider: {request.ProviderName} ({request.ProviderId})
                - Specialty: {request.Specialty}
                - Diagnosis: {request.DiagnosisCode}, Procedure: {request.ProcedureCode}
                - Amount: ${request.Amount:N2}
                - Service Date: {request.ServiceDate:yyyy-MM-dd}
                - Location: {request.Location}
                - Validation Errors: {validationResponse.Errors.Count}
                {string.Join("\n", validationResponse.Errors.Select(e => $"  - [{e.Code}] {e.Field}: {e.Message}"))}
                - Warnings: {validationResponse.Warnings.Count}
                {string.Join("\n", validationResponse.Warnings.Select(w => $"  - {w}"))}
                - Risk Score: {validationResponse.RiskScore} ({validationResponse.RiskCategory})
                - Risk Violations: {validationResponse.RiskViolations.Count}
                {string.Join("\n", validationResponse.RiskViolations.Select(v => $"  - {v.RuleName} ({v.Severity}): {v.Description}"))}

                Provide a brief validation assessment, highlight any parameters that pose risk in approving this claim, and give a recommendation (approve/reject/review).
                """;

            return await CallAgentAsync(prompt, "ClaimValidationAnalysis");
        }
        catch (Exception ex)
        {
            LogToFile($"ERROR [AnalyzeClaimValidationAsync]: {ex}");
            _logger.LogError(ex, "Error calling Azure Foundry agent for claim validation analysis");
            return string.Empty;
        }
    }

    public async Task<string> AnalyzeDocumentAsync(DocumentValidationRequest request, List<DocumentIssue> issues)
    {
        try
        {
            var prompt = $"""
                Analyze the following claim document for validity:
                - Document ID: {request.DocumentId}
                - Claim ID: {request.ClaimId}
                - File: {request.FileName}
                - Type: {request.DocumentType}
                - Validation Issues Found: {issues.Count}
                {string.Join("\n", issues.Select(i => $"  - [{i.Severity}] {i.Code}: {i.Message}"))}

                Provide a brief document validity assessment and any recommendations.
                """;

            return await CallAgentAsync(prompt, "DocumentAnalysis");
        }
        catch (Exception ex)
        {
            LogToFile($"ERROR [AnalyzeDocumentAsync]: {ex}");
            _logger.LogError(ex, "Error calling Azure Foundry agent for document analysis");
            return string.Empty;
        }
    }

    private async Task<string> CallAgentAsync(string prompt, string context)
    {
        var endpoint = _configuration["AzureFoundry:Endpoint"];
        var agentName = _configuration["AzureFoundry:AgentName"];
        var agentVersion = _configuration["AzureFoundry:AgentVersion"];

        if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(agentName))
        {
            _logger.LogWarning("Azure Foundry endpoint or AgentName not configured. Skipping agent analysis.");
            LogToFile($"WARN [{context}]: Azure Foundry endpoint or AgentName not configured. Skipping.");
            return string.Empty;
        }

        LogToFile($"INFO [{context}]: Calling agent '{agentName}' v{agentVersion} at {endpoint}");

        // Use ManagedIdentityCredential (system-assigned) on App Service, falls back to CLI/VS locally
        var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            ManagedIdentityClientId = null // Use system-assigned managed identity
        });

        // Log which identity is being used
        try
        {
            var tokenRequest = new Azure.Core.TokenRequestContext(["https://cognitiveservices.azure.com/.default"]);
            var token = await credential.GetTokenAsync(tokenRequest);
            var tokenParts = token.Token.Split('.');
            if (tokenParts.Length > 1)
            {
                var payload = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(tokenParts[1].PadRight(tokenParts[1].Length + (4 - tokenParts[1].Length % 4) % 4, '=')));
                LogToFile($"INFO [{context}]: Token identity: {payload[..Math.Min(200, payload.Length)]}...");
            }
        }
        catch (Exception ex)
        {
            LogToFile($"WARN [{context}]: Could not decode token: {ex.Message}");
        }

        AIProjectClient projectClient = new(endpoint: new Uri(endpoint), tokenProvider: credential);

        AgentReference agentReference = new(name: agentName, version: agentVersion ?? "1");
        ProjectResponsesClient responseClient = projectClient.OpenAI.GetProjectResponsesClientForAgent(agentReference);

        ResponseResult response = await responseClient.CreateResponseAsync(prompt);
        var outputText = response.GetOutputText();

        LogToFile($"INFO [{context}]: Agent response received ({outputText.Length} chars)");
        return outputText;
    }

    private void LogToFile(string message)
    {
        try
        {
            var logPath = Path.Combine(LogDirectory, LogFileName);
            var logEntry = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff UTC}] {message}{Environment.NewLine}";
            File.AppendAllText(logPath, logEntry);
        }
        catch
        {
            // Don't let logging failures affect the main flow
        }
    }
}

