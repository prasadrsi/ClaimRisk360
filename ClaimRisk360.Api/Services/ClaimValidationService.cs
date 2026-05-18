using ClaimRisk360.Api.Models;

namespace ClaimRisk360.Api.Services;

/// <summary>
/// Service that handles claim validation logic (moved from ClaimRisk360 main project).
/// Validates claim fields, business rules, and reference data.
/// </summary>
public class ClaimValidationService
{
    private static readonly HashSet<string> ValidDiagnosisCodes =
    [
        "J06.9", "M54.5", "I10", "E11.9", "J18.9", "K21.0", "N39.0",
        "R10.9", "M79.3", "J02.9", "Z00.00", "Z23", "G43.909", "F32.9",
        "J45.20", "E78.5", "K58.9", "L70.0", "H10.9", "B34.9"
    ];

    private static readonly HashSet<string> ValidProcedureCodes =
    [
        "99213", "99214", "99215", "99203", "99204", "99205",
        "36415", "71046", "80053", "85025", "87880", "90471",
        "90715", "93000", "97110", "99232", "99233", "99281",
        "99282", "99283", "99284", "99285", "99291"
    ];

    private static readonly HashSet<string> ActiveProviders =
    [
        "PRV001", "PRV002", "PRV003", "PRV004", "PRV005",
        "PRV006", "PRV007", "PRV008", "PRV009", "PRV010",
        "PRV011", "PRV012", "PRV013", "PRV014", "PRV015"
    ];

    public ClaimValidationResponse Validate(ClaimValidationRequest request)
    {
        var response = new ClaimValidationResponse();

        // Schema & mandatory field validation
        if (string.IsNullOrWhiteSpace(request.PatientName))
            response.Errors.Add(new() { Field = "PatientName", Code = "REQUIRED", Message = "Patient name is required" });

        if (string.IsNullOrWhiteSpace(request.PatientId))
            response.Errors.Add(new() { Field = "PatientId", Code = "REQUIRED", Message = "Patient ID is required" });

        if (string.IsNullOrWhiteSpace(request.ProviderName))
            response.Errors.Add(new() { Field = "ProviderName", Code = "REQUIRED", Message = "Provider name is required" });

        if (string.IsNullOrWhiteSpace(request.ProviderId))
            response.Errors.Add(new() { Field = "ProviderId", Code = "REQUIRED", Message = "Provider ID is required" });

        if (string.IsNullOrWhiteSpace(request.DiagnosisCode))
            response.Errors.Add(new() { Field = "DiagnosisCode", Code = "REQUIRED", Message = "Diagnosis code is required" });

        if (string.IsNullOrWhiteSpace(request.ProcedureCode))
            response.Errors.Add(new() { Field = "ProcedureCode", Code = "REQUIRED", Message = "Procedure code is required" });

        if (request.Amount <= 0)
            response.Errors.Add(new() { Field = "Amount", Code = "INVALID_AMOUNT", Message = "Amount must be greater than zero" });

        // Date validation
        if (request.ServiceDate > DateTime.Today)
            response.Errors.Add(new() { Field = "ServiceDate", Code = "FUTURE_DATE", Message = "Service date cannot be in the future" });

        if (request.ServiceDate < DateTime.Today.AddYears(-1))
            response.Errors.Add(new() { Field = "ServiceDate", Code = "STALE_CLAIM", Message = "Service date is older than 1 year" });

        // Business validation using reference data
        if (!string.IsNullOrWhiteSpace(request.DiagnosisCode) && !ValidDiagnosisCodes.Contains(request.DiagnosisCode))
            response.Errors.Add(new() { Field = "DiagnosisCode", Code = "INVALID_CODE", Message = $"Diagnosis code '{request.DiagnosisCode}' is not recognized" });

        if (!string.IsNullOrWhiteSpace(request.ProcedureCode) && !ValidProcedureCodes.Contains(request.ProcedureCode))
            response.Errors.Add(new() { Field = "ProcedureCode", Code = "INVALID_CODE", Message = $"Procedure code '{request.ProcedureCode}' is not recognized" });

        if (!string.IsNullOrWhiteSpace(request.ProviderId) && !ActiveProviders.Contains(request.ProviderId))
            response.Errors.Add(new() { Field = "ProviderId", Code = "INACTIVE_PROVIDER", Message = "Provider is not currently enrolled/active" });

        if (request.Amount > 50000)
            response.Warnings.Add("Claim amount exceeds $50,000 threshold — will require additional review");

        if (string.IsNullOrWhiteSpace(request.Location))
            response.Warnings.Add("Location not provided — geographic analysis will be limited");

        return response;
    }
}
