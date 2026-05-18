using ClaimRisk360.Data;
using ClaimRisk360.Models;

namespace ClaimRisk360.Services;

/// <summary>
/// Business Logic: claim validation rules.
/// Delegates to ClaimRisk360.Api for validation and AI-powered analysis.
/// Falls back to local validation if the API is unavailable.
/// </summary>
public class ClaimValidationService
{
    private readonly ReferenceDataRepository _refData;
    private readonly ClaimRisk360ApiClient _apiClient;
    private readonly ILogger<ClaimValidationService> _logger;

    public ClaimValidationService(ReferenceDataRepository refData, ClaimRisk360ApiClient apiClient, ILogger<ClaimValidationService> logger)
    {
        _refData = refData;
        _apiClient = apiClient;
        _logger = logger;
    }

    /// <summary>
    /// Validate via the ClaimRisk360 API (includes Azure Foundry agent analysis).
    /// Falls back to local validation on failure.
    /// </summary>
    public async Task<ValidationResult> ValidateAsync(ClaimUploadRequest request)
    {
        var apiResult = await _apiClient.ValidateClaimAsync(request);
        if (apiResult is not null)
        {
            // Map API response to local model
            var result = new ValidationResult();
            foreach (var err in apiResult.Errors)
                result.Errors.Add(new ValidationError { Field = err.Field, Code = err.Code, Message = err.Message });
            result.Warnings.AddRange(apiResult.Warnings);
            result.RiskScore = apiResult.RiskScore;
            result.RiskCategory = apiResult.RiskCategory;
            foreach (var v in apiResult.RiskViolations)
                result.RiskViolations.Add(new RiskViolation { RuleName = v.RuleName, Severity = v.Severity, Description = v.Description, Triggered = v.Triggered });
            foreach (var f in apiResult.FeatureContributions)
                result.FeatureContributions.Add(new RiskFeatureContribution { FeatureName = f.FeatureName, Contribution = f.Contribution });
            return result;
        }

        _logger.LogWarning("API unavailable, using local claim validation");
        return Validate(request);
    }

    /// <summary>
    /// Local validation (synchronous fallback).
    /// </summary>
    public ValidationResult Validate(ClaimUploadRequest request)
    {
        var result = new ValidationResult();

        // Schema & mandatory field validation
        if (string.IsNullOrWhiteSpace(request.PatientName))
            result.Errors.Add(new() { Field = "PatientName", Code = "REQUIRED", Message = "Patient name is required" });

        if (string.IsNullOrWhiteSpace(request.PatientId))
            result.Errors.Add(new() { Field = "PatientId", Code = "REQUIRED", Message = "Patient ID is required" });

        if (string.IsNullOrWhiteSpace(request.ProviderName))
            result.Errors.Add(new() { Field = "ProviderName", Code = "REQUIRED", Message = "Provider name is required" });

        if (string.IsNullOrWhiteSpace(request.ProviderId))
            result.Errors.Add(new() { Field = "ProviderId", Code = "REQUIRED", Message = "Provider ID is required" });

        if (string.IsNullOrWhiteSpace(request.DiagnosisCode))
            result.Errors.Add(new() { Field = "DiagnosisCode", Code = "REQUIRED", Message = "Diagnosis code is required" });

        if (string.IsNullOrWhiteSpace(request.ProcedureCode))
            result.Errors.Add(new() { Field = "ProcedureCode", Code = "REQUIRED", Message = "Procedure code is required" });

        if (request.Amount <= 0)
            result.Errors.Add(new() { Field = "Amount", Code = "INVALID_AMOUNT", Message = "Amount must be greater than zero" });

        // Date validation
        if (request.ServiceDate > DateTime.Today)
            result.Errors.Add(new() { Field = "ServiceDate", Code = "FUTURE_DATE", Message = "Service date cannot be in the future" });

        if (request.ServiceDate < DateTime.Today.AddYears(-1))
            result.Errors.Add(new() { Field = "ServiceDate", Code = "STALE_CLAIM", Message = "Service date is older than 1 year" });

        // Business validation using reference data
        if (!string.IsNullOrWhiteSpace(request.DiagnosisCode) && !_refData.ValidDiagnosisCodes.Contains(request.DiagnosisCode))
            result.Errors.Add(new() { Field = "DiagnosisCode", Code = "INVALID_CODE", Message = $"Diagnosis code '{request.DiagnosisCode}' is not recognized" });

        if (!string.IsNullOrWhiteSpace(request.ProcedureCode) && !_refData.ValidProcedureCodes.Contains(request.ProcedureCode))
            result.Errors.Add(new() { Field = "ProcedureCode", Code = "INVALID_CODE", Message = $"Procedure code '{request.ProcedureCode}' is not recognized" });

        if (!string.IsNullOrWhiteSpace(request.ProviderId) && !_refData.ActiveProviders.Contains(request.ProviderId))
            result.Errors.Add(new() { Field = "ProviderId", Code = "INACTIVE_PROVIDER", Message = "Provider is not currently enrolled/active" });

        if (request.Amount > 50000)
            result.Warnings.Add("Claim amount exceeds $50,000 threshold — will require additional review");

        if (string.IsNullOrWhiteSpace(request.Location))
            result.Warnings.Add("Location not provided — geographic analysis will be limited");

        return result;
    }
}
