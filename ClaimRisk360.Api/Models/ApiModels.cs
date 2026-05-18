namespace ClaimRisk360.Api.Models;

public class ClaimRuleEvaluationRequest
{
    public string ClaimId { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public string ProviderId { get; set; } = string.Empty;
    public string Specialty { get; set; } = string.Empty;
    public string DiagnosisCode { get; set; } = string.Empty;
    public string ProcedureCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime ServiceDate { get; set; }
    public string Location { get; set; } = string.Empty;
}

public class ClaimRuleEvaluationResponse
{
    public string ClaimId { get; set; } = string.Empty;
    public bool HasViolations { get; set; }
    public int RiskScore { get; set; }
    public string RiskCategory { get; set; } = string.Empty;
    public List<RuleViolation> Violations { get; set; } = [];
    public List<FeatureContribution> FeatureContributions { get; set; } = [];
    public string AgentAnalysis { get; set; } = string.Empty;
    public DateTime EvaluatedAt { get; set; } = DateTime.UtcNow;
}

public class RuleViolation
{
    public string RuleName { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Triggered { get; set; }
}

public class FeatureContribution
{
    public string FeatureName { get; set; } = string.Empty;
    public double Contribution { get; set; }
    public string Impact => Contribution >= 0 ? "Increases Risk" : "Decreases Risk";
}

public class DocumentValidationRequest
{
    public string ClaimId { get; set; } = string.Empty;
    public string DocumentId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string ContentBase64 { get; set; } = string.Empty;
}

public class DocumentValidationResponse
{
    public string DocumentId { get; set; } = string.Empty;
    public string ClaimId { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<DocumentIssue> Issues { get; set; } = [];
    public string AgentAnalysis { get; set; } = string.Empty;
    public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;
}

public class DocumentIssue
{
    public string Code { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class ClaimValidationRequest
{
    public string PatientName { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public string ProviderId { get; set; } = string.Empty;
    public string Specialty { get; set; } = string.Empty;
    public string DiagnosisCode { get; set; } = string.Empty;
    public string ProcedureCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime ServiceDate { get; set; } = DateTime.Today;
    public string Location { get; set; } = string.Empty;
}

public class ClaimValidationResponse
{
    public bool IsValid => Errors.Count == 0;
    public string Status => IsValid ? "Passed" : "Rejected";
    public List<ClaimValidationError> Errors { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
    public string AgentAnalysis { get; set; } = string.Empty;
    public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;

    // Risk assessment from rule evaluation
    public int RiskScore { get; set; }
    public string RiskCategory { get; set; } = string.Empty;
    public List<RuleViolation> RiskViolations { get; set; } = [];
    public List<FeatureContribution> FeatureContributions { get; set; } = [];
}

public class ClaimValidationError
{
    public string Field { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
