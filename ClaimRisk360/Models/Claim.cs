using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClaimRisk360.Models;

public class Claim
{
    [Key]
    public string ClaimId { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public string ProviderId { get; set; } = string.Empty;
    public string Specialty { get; set; } = string.Empty;
    public string DiagnosisCode { get; set; } = string.Empty;
    public string ProcedureCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime SubmissionDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public int FraudRiskScore { get; set; }

    [NotMapped]
    public string RiskCategory => FraudRiskScore switch
    {
        <= 30 => "Low",
        <= 70 => "Medium",
        _ => "High"
    };

    [NotMapped]
    public string RiskBadgeClass => RiskCategory switch
    {
        "Low" => "bg-success",
        "Medium" => "bg-warning text-dark",
        _ => "bg-danger"
    };

    /// <summary>Stored as JSON in SQLite.</summary>
    public List<string> RiskReasons { get; set; } = [];
    public string FraudType { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";

    // Approval tracking
    public string ApprovalStatus { get; set; } = "Pending";
    public string ApprovalMethod { get; set; } = string.Empty;
    public string ApprovedBy { get; set; } = string.Empty;
    public string ApprovalComment { get; set; } = string.Empty;
    public DateTime? ApprovedAt { get; set; }

    [NotMapped]
    public string ApprovalBadgeClass => ApprovalStatus switch
    {
        "Auto-Approved" => "bg-success",
        "Approved" => "bg-success",
        "Rejected" => "bg-danger",
        "Pending Review" => "bg-warning text-dark",
        _ => "bg-secondary"
    };

    [NotMapped]
    public string ApprovalIcon => ApprovalStatus switch
    {
        "Auto-Approved" => "bi-lightning-fill",
        "Approved" => "bi-check-circle-fill",
        "Rejected" => "bi-x-circle-fill",
        "Pending Review" => "bi-hourglass-split",
        _ => "bi-question-circle"
    };
}

public class FraudRing
{
    [Key]
    public string RingId { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public List<GraphNode> Nodes { get; set; } = [];
    public List<GraphEdge> Edges { get; set; } = [];
    public int ClaimCount { get; set; }
    public decimal TotalAmount { get; set; }
    public int RiskScore { get; set; }
}

public class GraphNode
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int GraphNodeId { get; set; }
    public string Id { get; set; } = string.Empty;
    public string FraudRingId { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
}

public class GraphEdge
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int GraphEdgeId { get; set; }
    public string FraudRingId { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty;
    public int Weight { get; set; } = 1;
}

public class ExplainabilityResult
{
    public string ClaimId { get; set; } = string.Empty;
    public int FraudRiskScore { get; set; }
    public List<FeatureContribution> Features { get; set; } = [];
    public string ModelUsed { get; set; } = string.Empty;
    public string Decision { get; set; } = string.Empty;
}

public class FeatureContribution
{
    public string FeatureName { get; set; } = string.Empty;
    public double Contribution { get; set; }
    [NotMapped]
    public string Direction => Contribution >= 0 ? "Increases Risk" : "Decreases Risk";
    [NotMapped]
    public string BarClass => Contribution >= 0 ? "bg-danger" : "bg-success";
}

public class DashboardStats
{
    public int TotalClaims { get; set; }
    public int FlaggedClaims { get; set; }
    public int HighRiskClaims { get; set; }
    public decimal TotalAmountAtRisk { get; set; }
    public int FraudRingsDetected { get; set; }
    public double FalsePositiveRate { get; set; }
    public List<int> ScoreDistribution { get; set; } = [];
    public List<FraudTypeCount> FraudTypeCounts { get; set; } = [];
}

public class FraudTypeCount
{
    public string Type { get; set; } = string.Empty;
    public int Count { get; set; }
}
