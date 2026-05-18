using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClaimRisk360.Models;

public class ClaimUploadRequest
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

public class ValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public List<ValidationError> Errors { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
    [NotMapped]
    public string Status => IsValid ? "Passed" : "Rejected";

    // Risk assessment from rule evaluation
    public int RiskScore { get; set; }
    public string RiskCategory { get; set; } = string.Empty;
    public List<RiskViolation> RiskViolations { get; set; } = [];
    public List<RiskFeatureContribution> FeatureContributions { get; set; } = [];
}

public class RiskViolation
{
    public string RuleName { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Triggered { get; set; }
}

public class RiskFeatureContribution
{
    public string FeatureName { get; set; } = string.Empty;
    public double Contribution { get; set; }
    public string Impact => Contribution >= 0 ? "Increases Risk" : "Decreases Risk";
}

public class ValidationError
{
    public string Field { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class AuditEntry
{
    [Key]
    public string AuditId { get; set; } = string.Empty;
    public string ClaimId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string PerformedBy { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Details { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;

    // FK for CaseReview history
    public string? CaseReviewId { get; set; }
}

public class CaseReview
{
    [Key]
    public string CaseId { get; set; } = string.Empty;
    public string ClaimId { get; set; } = string.Empty;
    public string AssignedTo { get; set; } = string.Empty;
    public string Status { get; set; } = "Open";
    public string Priority { get; set; } = "Medium";
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string Decision { get; set; } = string.Empty;
    public string Justification { get; set; } = string.Empty;
    public List<AuditEntry> History { get; set; } = [];

    [NotMapped]
    public string StatusBadgeClass => Status switch
    {
        "Open" => "bg-warning text-dark",
        "In Review" => "bg-info",
        "Escalated" => "bg-danger",
        "Resolved" => "bg-success",
        _ => "bg-secondary"
    };

    [NotMapped]
    public string PriorityBadgeClass => Priority switch
    {
        "Critical" => "bg-danger",
        "High" => "bg-warning text-dark",
        "Medium" => "bg-info",
        "Low" => "bg-success",
        _ => "bg-secondary"
    };
}
