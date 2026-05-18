using ClaimRisk360.Api.Models;

namespace ClaimRisk360.Api.Services;

/// <summary>
/// Service that evaluates claim rules for fraud detection.
/// </summary>
public class ClaimRuleEvaluationService
{
    private static readonly HashSet<string> BlacklistedProviders = ["PRV099", "PRV088", "PRV077"];
    private static readonly HashSet<string> BlacklistedPatients = ["PAT999", "PAT888"];

    public ClaimRuleEvaluationResponse Evaluate(ClaimRuleEvaluationRequest request)
    {
        var response = new ClaimRuleEvaluationResponse
        {
            ClaimId = request.ClaimId
        };

        // Rule 1: Blacklisted provider
        if (BlacklistedProviders.Contains(request.ProviderId))
        {
            response.Violations.Add(new RuleViolation
            {
                RuleName = "Blacklisted Provider",
                Severity = "Critical",
                Description = $"Provider '{request.ProviderId}' is on the blacklist",
                Triggered = true
            });
        }

        // Rule 2: Blacklisted patient
        if (BlacklistedPatients.Contains(request.PatientId))
        {
            response.Violations.Add(new RuleViolation
            {
                RuleName = "Blacklisted Patient",
                Severity = "High",
                Description = $"Patient '{request.PatientId}' is on the blacklist",
                Triggered = true
            });
        }

        // Rule 3: High amount threshold
        if (request.Amount > 25000)
        {
            response.Violations.Add(new RuleViolation
            {
                RuleName = "High Amount Threshold",
                Severity = request.Amount > 50000 ? "Critical" : "High",
                Description = $"Claim amount ${request.Amount:N2} exceeds threshold",
                Triggered = true
            });
        }

        // Rule 4: Weekend submission
        if (request.ServiceDate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            response.Violations.Add(new RuleViolation
            {
                RuleName = "Weekend Service",
                Severity = "Medium",
                Description = "Service performed on weekend — unusual pattern",
                Triggered = true
            });
        }

        // Rule 5: Future service date
        if (request.ServiceDate > DateTime.Today)
        {
            response.Violations.Add(new RuleViolation
            {
                RuleName = "Future Service Date",
                Severity = "Critical",
                Description = "Service date is in the future",
                Triggered = true
            });
        }

        // Calculate risk score
        response.HasViolations = response.Violations.Count > 0;
        response.RiskScore = CalculateRiskScore(response.Violations);
        response.RiskCategory = response.RiskScore switch
        {
            <= 30 => "Low",
            <= 70 => "Medium",
            _ => "High"
        };

        // Generate feature contributions (explainability)
        response.FeatureContributions = GenerateFeatureContributions(request, response.RiskScore);

        return response;
    }

    private static List<FeatureContribution> GenerateFeatureContributions(ClaimRuleEvaluationRequest request, int riskScore)
    {
        var features = new List<FeatureContribution>
        {
            new() { FeatureName = "Billing Frequency", Contribution = riskScore > 50 ? 0.32 : -0.15 },
            new() { FeatureName = "Amount vs Peer Average", Contribution = request.Amount > 8000 ? 0.28 : -0.10 },
            new() { FeatureName = "Provider Network Density", Contribution = BlacklistedProviders.Contains(request.ProviderId) ? 0.45 : -0.05 },
            new() { FeatureName = "Diagnosis-Procedure Match", Contribution = riskScore > 70 ? 0.22 : -0.20 },
            new() { FeatureName = "Temporal Pattern", Contribution = request.ServiceDate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday ? 0.18 : -0.08 },
            new() { FeatureName = "Geographic Consistency", Contribution = riskScore > 80 ? 0.15 : -0.12 },
            new() { FeatureName = "Patient History", Contribution = BlacklistedPatients.Contains(request.PatientId) ? 0.25 : -0.10 },
            new() { FeatureName = "Specialty Norm Deviation", Contribution = request.Amount > 25000 ? 0.20 : -0.05 }
        };
        return features.OrderByDescending(f => Math.Abs(f.Contribution)).ToList();
    }

    private static int CalculateRiskScore(List<RuleViolation> violations)
    {
        var score = 0;
        foreach (var v in violations.Where(v => v.Triggered))
        {
            score += v.Severity switch
            {
                "Critical" => 30,
                "High" => 20,
                "Medium" => 10,
                _ => 5
            };
        }
        return Math.Min(score, 100);
    }
}
