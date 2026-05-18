using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClaimRisk360.Models;

public class RuleCheckResult
{
    [Key]
    public string RuleId { get; set; } = string.Empty;
    public string RuleName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Severity { get; set; } = "Info";
    public bool Triggered { get; set; }
    public string Details { get; set; } = string.Empty;

    [NotMapped]
    public string SeverityBadgeClass => Severity switch
    {
        "Critical" => "bg-danger",
        "High" => "bg-warning text-dark",
        "Medium" => "bg-info",
        "Low" => "bg-success",
        _ => "bg-secondary"
    };

    [NotMapped]
    public string IconClass => Category switch
    {
        "Duplicate" => "bi-files",
        "Threshold" => "bi-graph-up-arrow",
        "Blacklist" => "bi-slash-circle",
        "Eligibility" => "bi-shield-exclamation",
        "Timing" => "bi-clock-history",
        "Document" => "bi-file-earmark-x",
        _ => "bi-exclamation-triangle"
    };
}

public class ProviderProfile
{
    [Key]
    public string ProviderId { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public string Specialty { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int TotalClaims { get; set; }
    public decimal TotalBilled { get; set; }
    public decimal AvgClaimAmount { get; set; }
    public decimal PeerAvgAmount { get; set; }
    public double DeviationPercent { get; set; }
    public int FlaggedClaims { get; set; }
    public double FlagRate { get; set; }
    public int RiskScore { get; set; }
    public string RiskLevel { get; set; } = "Low";
    /// <summary>Stored as JSON in SQLite.</summary>
    public List<string> RiskIndicators { get; set; } = [];

    [NotMapped]
    public string RiskBadgeClass => RiskLevel switch
    {
        "High" => "bg-danger",
        "Medium" => "bg-warning text-dark",
        _ => "bg-success"
    };
}

public class ClaimPattern
{
    [Key]
    public string PatternId { get; set; } = string.Empty;
    public string PatternType { get; set; } = string.Empty;
    public string Entity { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = "Medium";
    public int Occurrences { get; set; }
    public string TimeFrame { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; }

    [NotMapped]
    public string SeverityBadgeClass => Severity switch
    {
        "Critical" => "bg-danger",
        "High" => "bg-warning text-dark",
        "Medium" => "bg-info",
        _ => "bg-success"
    };

    [NotMapped]
    public string IconClass => PatternType switch
    {
        "Frequency Spike" => "bi-graph-up-arrow",
        "Timing Anomaly" => "bi-clock-history",
        "Amount Anomaly" => "bi-currency-dollar",
        "Duplicate Pattern" => "bi-files",
        "Geographic Anomaly" => "bi-geo-alt",
        "Behavioral" => "bi-person-exclamation",
        _ => "bi-exclamation-triangle"
    };
}

public class DigitalRiskSignal
{
    [Key]
    public string SignalId { get; set; } = string.Empty;
    public string ClaimId { get; set; } = string.Empty;
    public string SignalType { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string GeoLocation { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public string RiskLevel { get; set; } = "Low";
    public string Details { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; }

    [NotMapped]
    public string RiskBadgeClass => RiskLevel switch
    {
        "Critical" => "bg-danger",
        "High" => "bg-warning text-dark",
        "Medium" => "bg-info",
        _ => "bg-success"
    };

    [NotMapped]
    public string IconClass => SignalType switch
    {
        "Device Reuse" => "bi-phone",
        "VPN/Proxy" => "bi-shield-shaded",
        "Geo Mismatch" => "bi-geo-alt-fill",
        "Rapid Submission" => "bi-lightning",
        "Bot Pattern" => "bi-robot",
        "IP Anomaly" => "bi-wifi",
        _ => "bi-exclamation-diamond"
    };
}

public class StpDecision
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int StpDecisionId { get; set; }
    public string ClaimId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public int RiskScore { get; set; }
    public int RulesFired { get; set; }
    public int DigitalRiskFlags { get; set; }
    public DateTime ProcessedAt { get; set; }

    [NotMapped]
    public string ActionBadgeClass => Action switch
    {
        "Auto-Approved" => "bg-success",
        "Auto-Rejected" => "bg-danger",
        "Routed to Review" => "bg-warning text-dark",
        _ => "bg-secondary"
    };

    [NotMapped]
    public string ActionIcon => Action switch
    {
        "Auto-Approved" => "bi-check-circle-fill",
        "Auto-Rejected" => "bi-x-circle-fill",
        "Routed to Review" => "bi-person-lines-fill",
        _ => "bi-question-circle"
    };
}
