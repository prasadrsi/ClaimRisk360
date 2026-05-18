using ClaimRisk360.Data;
using ClaimRisk360.Models;

namespace ClaimRisk360.Services;

/// <summary>
/// Business Logic: fraud detection, risk scoring, and explainability.
/// Data is loaded from ClaimRepository (Data Layer).
/// Includes async methods for performance optimization.
/// </summary>
public class FraudDetectionService
{
    private readonly ClaimRepository _claimRepo;

    public FraudDetectionService(ClaimRepository claimRepo)
    {
        _claimRepo = claimRepo;
    }

    #region Synchronous Methods (Legacy)
    public List<Claim> GetAllClaims() => _claimRepo.GetAllClaims();

    public Claim? GetClaim(string claimId) => _claimRepo.GetClaim(claimId);

    public DashboardStats GetDashboardStats()
    {
        var claims = _claimRepo.GetAllClaims();
        var scoreDistribution = new int[10];
        foreach (var c in claims)
            scoreDistribution[Math.Min(c.FraudRiskScore / 10, 9)]++;

        return new DashboardStats
        {
            TotalClaims = claims.Count,
            FlaggedClaims = claims.Count(c => c.FraudRiskScore > 30),
            HighRiskClaims = claims.Count(c => c.FraudRiskScore > 70),
            TotalAmountAtRisk = claims.Where(c => c.FraudRiskScore > 70).Sum(c => c.Amount),
            FraudRingsDetected = _claimRepo.GetAllFraudRings().Count,
            FalsePositiveRate = 4.2,
            ScoreDistribution = scoreDistribution.ToList(),
            FraudTypeCounts =
            [
                new() { Type = "Provider Fraud", Count = claims.Count(c => c.FraudType == "Provider") },
                new() { Type = "Patient Fraud", Count = claims.Count(c => c.FraudType == "Patient") },
                new() { Type = "Pharmacy Fraud", Count = claims.Count(c => c.FraudType == "Pharmacy") },
                new() { Type = "Collusion", Count = claims.Count(c => c.FraudType == "Collusion") },
                new() { Type = "Legitimate", Count = claims.Count(c => c.FraudType == "Legitimate") }
            ]
        };
    }

    public List<FraudRing> GetFraudRings() => _claimRepo.GetAllFraudRings();

    public FraudRing? GetFraudRing(string ringId) => _claimRepo.GetFraudRing(ringId);

    public ExplainabilityResult GetExplainability(string claimId)
    {
        var claim = GetClaim(claimId);
        if (claim is null) return new ExplainabilityResult();

        return new ExplainabilityResult
        {
            ClaimId = claim.ClaimId,
            FraudRiskScore = claim.FraudRiskScore,
            ModelUsed = "Isolation Forest + Graph Neural Network",
            Decision = claim.RiskCategory switch
            {
                "High" => "Flag for Manual Audit",
                "Medium" => "Monitor",
                _ => "Auto-Approve"
            },
            Features = GenerateFeatureContributions(claim)
        };
    }
    #endregion

    #region Async Methods (New - Performance Optimized)

    /// <summary>
    /// Get paginated claims asynchronously
    /// </summary>
    public async Task<PaginatedResult<Claim>> GetClaimsPaginatedAsync(int pageNumber = 1, int pageSize = 50) =>
        await _claimRepo.GetClaimsPaginatedAsync(pageNumber, pageSize);

    /// <summary>
    /// Get all claims asynchronously (use pagination for large datasets)
    /// </summary>
    public async Task<List<Claim>> GetAllClaimsAsync() => 
        await _claimRepo.GetAllClaimsAsync();

    /// <summary>
    /// Get single claim asynchronously
    /// </summary>
    public async Task<Claim?> GetClaimAsync(string claimId) => 
        await _claimRepo.GetClaimAsync(claimId);

    /// <summary>
    /// Get dashboard statistics asynchronously
    /// </summary>
    public async Task<DashboardStats> GetDashboardStatsAsync()
    {
        var claims = await _claimRepo.GetAllClaimsAsync();
        var scoreDistribution = new int[10];
        foreach (var c in claims)
            scoreDistribution[Math.Min(c.FraudRiskScore / 10, 9)]++;

        return new DashboardStats
        {
            TotalClaims = claims.Count,
            FlaggedClaims = claims.Count(c => c.FraudRiskScore > 30),
            HighRiskClaims = claims.Count(c => c.FraudRiskScore > 70),
            TotalAmountAtRisk = claims.Where(c => c.FraudRiskScore > 70).Sum(c => c.Amount),
            FraudRingsDetected = (await _claimRepo.GetAllFraudRingsAsync()).Count,
            FalsePositiveRate = 4.2,
            ScoreDistribution = scoreDistribution.ToList(),
            FraudTypeCounts =
            [
                new() { Type = "Provider Fraud", Count = claims.Count(c => c.FraudType == "Provider") },
                new() { Type = "Patient Fraud", Count = claims.Count(c => c.FraudType == "Patient") },
                new() { Type = "Pharmacy Fraud", Count = claims.Count(c => c.FraudType == "Pharmacy") },
                new() { Type = "Collusion", Count = claims.Count(c => c.FraudType == "Collusion") },
                new() { Type = "Legitimate", Count = claims.Count(c => c.FraudType == "Legitimate") }
            ]
        };
    }

    /// <summary>
    /// Get all fraud rings asynchronously
    /// </summary>
    public async Task<List<FraudRing>> GetFraudRingsAsync() => 
        await _claimRepo.GetAllFraudRingsAsync();

    /// <summary>
    /// Get single fraud ring asynchronously
    /// </summary>
    public async Task<FraudRing?> GetFraudRingAsync(string ringId) => 
        await _claimRepo.GetFraudRingAsync(ringId);

    /// <summary>
    /// Get fraud ring summaries asynchronously (lightweight for list views)
    /// </summary>
    public async Task<List<(string RingId, string Name, int NodeCount, int EdgeCount)>> GetFraudRingSummariesAsync() =>
        await _claimRepo.GetFraudRingSummariesAsync();

    /// <summary>
    /// Get explainability result asynchronously
    /// </summary>
    public async Task<ExplainabilityResult> GetExplainabilityAsync(string claimId)
    {
        var claim = await GetClaimAsync(claimId);
        if (claim is null) return new ExplainabilityResult();

        return new ExplainabilityResult
        {
            ClaimId = claim.ClaimId,
            FraudRiskScore = claim.FraudRiskScore,
            ModelUsed = "Isolation Forest + Graph Neural Network",
            Decision = claim.RiskCategory switch
            {
                "High" => "Flag for Manual Audit",
                "Medium" => "Monitor",
                _ => "Auto-Approve"
            },
            Features = GenerateFeatureContributions(claim)
        };
    }

    #endregion

    private static List<FeatureContribution> GenerateFeatureContributions(Claim claim)
    {
        var features = new List<FeatureContribution>
        {
            new() { FeatureName = "Billing Frequency", Contribution = claim.FraudRiskScore > 50 ? 0.32 : -0.15 },
            new() { FeatureName = "Amount vs Peer Average", Contribution = claim.Amount > 8000 ? 0.28 : -0.10 },
            new() { FeatureName = "Provider Network Density", Contribution = claim.FraudType == "Collusion" ? 0.45 : -0.05 },
            new() { FeatureName = "Diagnosis-Procedure Match", Contribution = claim.FraudRiskScore > 70 ? 0.22 : -0.20 },
            new() { FeatureName = "Temporal Pattern", Contribution = claim.FraudRiskScore > 60 ? 0.18 : -0.08 },
            new() { FeatureName = "Geographic Consistency", Contribution = claim.FraudRiskScore > 80 ? 0.15 : -0.12 },
            new() { FeatureName = "Patient History", Contribution = -0.10 },
            new() { FeatureName = "Specialty Norm Deviation", Contribution = claim.FraudRiskScore > 50 ? 0.20 : -0.05 }
        };
        return features.OrderByDescending(f => Math.Abs(f.Contribution)).ToList();
    }
}
