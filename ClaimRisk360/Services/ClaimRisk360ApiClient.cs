using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ClaimRisk360.Models;

namespace ClaimRisk360.Services;

/// <summary>
/// HTTP client that calls the ClaimRisk360.Api project for claim validation,
/// rule evaluation, and document validation via the external Web API.
/// </summary>
public class ClaimRisk360ApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ClaimRisk360ApiClient> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public ClaimRisk360ApiClient(HttpClient httpClient, ILogger<ClaimRisk360ApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Validate a claim via the API.
    /// </summary>
    public async Task<ApiValidationResult?> ValidateClaimAsync(ClaimUploadRequest request)
    {
        try
        {
            var payload = JsonSerializer.Serialize(new
            {
                request.PatientName,
                request.PatientId,
                request.ProviderName,
                request.ProviderId,
                request.Specialty,
                request.DiagnosisCode,
                request.ProcedureCode,
                request.Amount,
                request.ServiceDate,
                request.Location
            }, JsonOptions);

            var response = await _httpClient.PostAsync(
                "api/ClaimValidation/validate",
                new StringContent(payload, Encoding.UTF8, "application/json"));

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ApiValidationResult>(json, JsonOptions);
            }

            _logger.LogWarning("ClaimRisk360 API validation returned {StatusCode}", response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling ClaimRisk360 API for claim validation");
        }

        return null;
    }

    /// <summary>
    /// Evaluate claim rules via the API.
    /// </summary>
    public async Task<ApiRuleEvaluationResult?> EvaluateRulesAsync(Claim claim)
    {
        try
        {
            var payload = JsonSerializer.Serialize(new
            {
                ClaimId = claim.ClaimId,
                PatientName = claim.PatientName,
                PatientId = claim.PatientId,
                ProviderName = claim.ProviderName,
                ProviderId = claim.ProviderId,
                Specialty = claim.Specialty,
                DiagnosisCode = claim.DiagnosisCode,
                ProcedureCode = claim.ProcedureCode,
                Amount = claim.Amount,
                ServiceDate = claim.SubmissionDate,
                Location = claim.Location
            }, JsonOptions);

            var response = await _httpClient.PostAsync(
                "api/ClaimRules/evaluate",
                new StringContent(payload, Encoding.UTF8, "application/json"));

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ApiRuleEvaluationResult>(json, JsonOptions);
            }

            _logger.LogWarning("ClaimRisk360 API rule evaluation returned {StatusCode}", response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling ClaimRisk360 API for rule evaluation");
        }

        return null;
    }

    /// <summary>
    /// Validate a document via the API.
    /// </summary>
    public async Task<ApiDocumentValidationResult?> ValidateDocumentAsync(string claimId, string documentId, string fileName, string documentType, string contentBase64)
    {
        try
        {
            var payload = JsonSerializer.Serialize(new
            {
                ClaimId = claimId,
                DocumentId = documentId,
                FileName = fileName,
                DocumentType = documentType,
                ContentBase64 = contentBase64
            }, JsonOptions);

            var response = await _httpClient.PostAsync(
                "api/DocumentValidation/validate",
                new StringContent(payload, Encoding.UTF8, "application/json"));

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ApiDocumentValidationResult>(json, JsonOptions);
            }

            _logger.LogWarning("ClaimRisk360 API document validation returned {StatusCode}", response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling ClaimRisk360 API for document validation");
        }

        return null;
    }
}

#region API Response Models

public class ApiValidationResult
{
    public bool IsValid { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<ApiValidationError> Errors { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
    public string AgentAnalysis { get; set; } = string.Empty;
    public int RiskScore { get; set; }
    public string RiskCategory { get; set; } = string.Empty;
    public List<ApiRuleViolation> RiskViolations { get; set; } = [];
    public List<ApiFeatureContribution> FeatureContributions { get; set; } = [];
}

public class ApiValidationError
{
    public string Field { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class ApiRuleEvaluationResult
{
    public string ClaimId { get; set; } = string.Empty;
    public bool HasViolations { get; set; }
    public int RiskScore { get; set; }
    public string RiskCategory { get; set; } = string.Empty;
    public List<ApiRuleViolation> Violations { get; set; } = [];
    public List<ApiFeatureContribution> FeatureContributions { get; set; } = [];
    public string AgentAnalysis { get; set; } = string.Empty;
}

public class ApiRuleViolation
{
    public string RuleName { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Triggered { get; set; }
}

public class ApiFeatureContribution
{
    public string FeatureName { get; set; } = string.Empty;
    public double Contribution { get; set; }
    public string Impact { get; set; } = string.Empty;
}

public class ApiDocumentValidationResult
{
    public string DocumentId { get; set; } = string.Empty;
    public string ClaimId { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<ApiDocumentIssue> Issues { get; set; } = [];
    public string AgentAnalysis { get; set; } = string.Empty;
}

public class ApiDocumentIssue
{
    public string Code { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

#endregion
