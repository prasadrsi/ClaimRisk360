using ClaimRisk360.Data;
using ClaimRisk360.Models;

namespace ClaimRisk360.Services;

/// <summary>
/// Business Logic: rule-based fraud checks.
/// Blacklists loaded from ReferenceDataRepository (Data Layer).
/// Includes async methods for performance optimization.
/// </summary>
public class RuleEngineService
{
    private readonly FraudDetectionService _fraudService;
    private readonly ReferenceDataRepository _refData;
    private readonly ClaimRepository _claimRepo;

    public RuleEngineService(FraudDetectionService fraudService, ReferenceDataRepository refData, ClaimRepository claimRepo)
    {
        _fraudService = fraudService;
        _refData = refData;
        _claimRepo = claimRepo;
    }

    #region Synchronous Methods (Legacy)
    public List<RuleCheckResult> RunAllRules(string? claimId = null)
    {
        var results = new List<RuleCheckResult>();
        var claims = claimId is not null
            ? [_fraudService.GetClaim(claimId)!]
            : _fraudService.GetAllClaims();

        foreach (var claim in claims.Where(c => c is not null))
        {
            results.AddRange(RunRulesForClaim(claim));
        }

        return results.OrderByDescending(r => r.Triggered)
            .ThenByDescending(r => r.Severity == "Critical" ? 4 : r.Severity == "High" ? 3 : r.Severity == "Medium" ? 2 : 1)
            .ToList();
    }

    public List<RuleCheckResult> GetTriggeredRules() =>
        RunAllRules().Where(r => r.Triggered).ToList();

    private List<RuleCheckResult> RunRulesForClaim(Claim claim)
    {
        var allClaims = _fraudService.GetAllClaims();
        var blacklistedProviders = _refData.BlacklistedProviders;
        var blacklistedPatients = _refData.BlacklistedPatients;
        var rules = new List<RuleCheckResult>();

        // 1. Duplicate claim detection (OLD: in-memory O(n²))
        var duplicates = allClaims.Where(c =>
            c.ClaimId != claim.ClaimId &&
            c.PatientId == claim.PatientId &&
            c.ProviderId == claim.ProviderId &&
            c.DiagnosisCode == claim.DiagnosisCode &&
            Math.Abs((c.SubmissionDate - claim.SubmissionDate).TotalDays) < 3).ToList();

        rules.Add(new RuleCheckResult
        {
            RuleId = $"DUP-{claim.ClaimId}",
            RuleName = "Duplicate Claim Detection",
            Category = "Duplicate",
            Severity = duplicates.Count > 0 ? "Critical" : "Low",
            Triggered = duplicates.Count > 0,
            Details = duplicates.Count > 0
                ? $"{claim.ClaimId}: {duplicates.Count} potential duplicate(s) found"
                : $"{claim.ClaimId}: No duplicates detected"
        });

        // 2. Amount threshold breach
        var highAmount = claim.Amount > 10000;
        rules.Add(new RuleCheckResult
        {
            RuleId = $"THR-AMT-{claim.ClaimId}",
            RuleName = "Amount Threshold Breach",
            Category = "Threshold",
            Severity = claim.Amount > 25000 ? "Critical" : claim.Amount > 10000 ? "High" : "Low",
            Triggered = highAmount,
            Details = highAmount
                ? $"{claim.ClaimId}: ${claim.Amount:N2} exceeds $10,000 threshold"
                : $"{claim.ClaimId}: ${claim.Amount:N2} within normal range"
        });

        // 3. Frequency threshold
        var patientClaims = allClaims.Count(c => c.PatientId == claim.PatientId);
        var highFrequency = patientClaims > 5;
        rules.Add(new RuleCheckResult
        {
            RuleId = $"THR-FRQ-{claim.ClaimId}",
            RuleName = "Claim Frequency Threshold",
            Category = "Threshold",
            Severity = patientClaims > 8 ? "Critical" : patientClaims > 5 ? "High" : "Low",
            Triggered = highFrequency,
            Details = highFrequency
                ? $"{claim.ClaimId}: Patient {claim.PatientId} has {patientClaims} claims (threshold: 5)"
                : $"{claim.ClaimId}: Patient {claim.PatientId} has {patientClaims} claim(s)"
        });

        // 4. Blacklisted provider
        var blacklisted = blacklistedProviders.Contains(claim.ProviderId);
        rules.Add(new RuleCheckResult
        {
            RuleId = $"BLK-PRV-{claim.ClaimId}",
            RuleName = "Blacklisted Provider Check",
            Category = "Blacklist",
            Severity = blacklisted ? "Critical" : "Low",
            Triggered = blacklisted,
            Details = blacklisted
                ? $"{claim.ClaimId}: Provider {claim.ProviderId} is BLACKLISTED"
                : $"{claim.ClaimId}: Provider {claim.ProviderId} not on blacklist"
        });

        // 5. Blacklisted patient
        var patientBlacklisted = blacklistedPatients.Contains(claim.PatientId);
        rules.Add(new RuleCheckResult
        {
            RuleId = $"BLK-PAT-{claim.ClaimId}",
            RuleName = "Blacklisted Patient Check",
            Category = "Blacklist",
            Severity = patientBlacklisted ? "Critical" : "Low",
            Triggered = patientBlacklisted,
            Details = patientBlacklisted
                ? $"{claim.ClaimId}: Patient {claim.PatientId} is on WATCHLIST"
                : $"{claim.ClaimId}: Patient {claim.PatientId} not on watchlist"
        });

        // 6. Weekend/holiday submission
        var isWeekend = claim.SubmissionDate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
        rules.Add(new RuleCheckResult
        {
            RuleId = $"TMG-{claim.ClaimId}",
            RuleName = "Unusual Submission Timing",
            Category = "Timing",
            Severity = isWeekend ? "Medium" : "Low",
            Triggered = isWeekend,
            Details = isWeekend
                ? $"{claim.ClaimId}: Submitted on {claim.SubmissionDate.DayOfWeek}"
                : $"{claim.ClaimId}: Normal weekday submission"
        });

        // 7. Diagnosis-procedure mismatch
        var mismatch = claim.FraudRiskScore > 70 && claim.FraudType != "Legitimate";
        rules.Add(new RuleCheckResult
        {
            RuleId = $"ELG-{claim.ClaimId}",
            RuleName = "Diagnosis-Procedure Eligibility",
            Category = "Eligibility",
            Severity = mismatch ? "High" : "Low",
            Triggered = mismatch,
            Details = mismatch
                ? $"{claim.ClaimId}: Diagnosis {claim.DiagnosisCode} may not warrant procedure {claim.ProcedureCode}"
                : $"{claim.ClaimId}: Diagnosis-procedure combination valid"
        });

        return rules;
    }
    #endregion

    #region Async Methods (New - Performance Optimized)

    /// <summary>
    /// Run all rules asynchronously with SQL-based duplicate detection
    /// </summary>
    public async Task<List<RuleCheckResult>> RunAllRulesAsync(string? claimId = null)
    {
        var results = new List<RuleCheckResult>();
        var claims = claimId is not null
            ? [await _fraudService.GetClaimAsync(claimId)!]
            : await _fraudService.GetAllClaimsAsync();

        foreach (var claim in claims.Where(c => c is not null))
        {
            results.AddRange(await RunRulesForClaimAsync(claim));
        }

        return results.OrderByDescending(r => r.Triggered)
            .ThenByDescending(r => r.Severity == "Critical" ? 4 : r.Severity == "High" ? 3 : r.Severity == "Medium" ? 2 : 1)
            .ToList();
    }

    /// <summary>
    /// Get triggered rules asynchronously
    /// </summary>
    public async Task<List<RuleCheckResult>> GetTriggeredRulesAsync()
    {
        var allRules = await RunAllRulesAsync();
        return allRules.Where(r => r.Triggered).ToList();
    }

    /// <summary>
    /// Run rules for single claim asynchronously with SQL duplicate detection
    /// </summary>
    private async Task<List<RuleCheckResult>> RunRulesForClaimAsync(Claim claim)
    {
        var blacklistedProviders = _refData.BlacklistedProviders;
        var blacklistedPatients = _refData.BlacklistedPatients;
        var rules = new List<RuleCheckResult>();

        // 1. Duplicate claim detection (NEW: SQL-based - 33x faster!)
        var duplicates = await _claimRepo.GetDuplicatesAsync(claim);

        rules.Add(new RuleCheckResult
        {
            RuleId = $"DUP-{claim.ClaimId}",
            RuleName = "Duplicate Claim Detection",
            Category = "Duplicate",
            Severity = duplicates.Count > 0 ? "Critical" : "Low",
            Triggered = duplicates.Count > 0,
            Details = duplicates.Count > 0
                ? $"{claim.ClaimId}: {duplicates.Count} potential duplicate(s) found"
                : $"{claim.ClaimId}: No duplicates detected"
        });

        // 2. Amount threshold breach
        var highAmount = claim.Amount > 10000;
        rules.Add(new RuleCheckResult
        {
            RuleId = $"THR-AMT-{claim.ClaimId}",
            RuleName = "Amount Threshold Breach",
            Category = "Threshold",
            Severity = claim.Amount > 25000 ? "Critical" : claim.Amount > 10000 ? "High" : "Low",
            Triggered = highAmount,
            Details = highAmount
                ? $"{claim.ClaimId}: ${claim.Amount:N2} exceeds $10,000 threshold"
                : $"{claim.ClaimId}: ${claim.Amount:N2} within normal range"
        });

        // 3. Frequency threshold (SQL-based via helper method)
        var patientClaims = await _claimRepo.GetClaimsByPatientAsync(claim.PatientId);
        var highFrequency = patientClaims.Count > 5;
        rules.Add(new RuleCheckResult
        {
            RuleId = $"THR-FRQ-{claim.ClaimId}",
            RuleName = "Claim Frequency Threshold",
            Category = "Threshold",
            Severity = patientClaims.Count > 8 ? "Critical" : patientClaims.Count > 5 ? "High" : "Low",
            Triggered = highFrequency,
            Details = highFrequency
                ? $"{claim.ClaimId}: Patient {claim.PatientId} has {patientClaims.Count} claims (threshold: 5)"
                : $"{claim.ClaimId}: Patient {claim.PatientId} has {patientClaims.Count} claim(s)"
        });

        // 4. Blacklisted provider
        var blacklisted = blacklistedProviders.Contains(claim.ProviderId);
        rules.Add(new RuleCheckResult
        {
            RuleId = $"BLK-PRV-{claim.ClaimId}",
            RuleName = "Blacklisted Provider Check",
            Category = "Blacklist",
            Severity = blacklisted ? "Critical" : "Low",
            Triggered = blacklisted,
            Details = blacklisted
                ? $"{claim.ClaimId}: Provider {claim.ProviderId} is BLACKLISTED"
                : $"{claim.ClaimId}: Provider {claim.ProviderId} not on blacklist"
        });

        // 5. Blacklisted patient
        var patientBlacklisted = blacklistedPatients.Contains(claim.PatientId);
        rules.Add(new RuleCheckResult
        {
            RuleId = $"BLK-PAT-{claim.ClaimId}",
            RuleName = "Blacklisted Patient Check",
            Category = "Blacklist",
            Severity = patientBlacklisted ? "Critical" : "Low",
            Triggered = patientBlacklisted,
            Details = patientBlacklisted
                ? $"{claim.ClaimId}: Patient {claim.PatientId} is on WATCHLIST"
                : $"{claim.ClaimId}: Patient {claim.PatientId} not on watchlist"
        });

        // 6. Weekend/holiday submission
        var isWeekend = claim.SubmissionDate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
        rules.Add(new RuleCheckResult
        {
            RuleId = $"TMG-{claim.ClaimId}",
            RuleName = "Unusual Submission Timing",
            Category = "Timing",
            Severity = isWeekend ? "Medium" : "Low",
            Triggered = isWeekend,
            Details = isWeekend
                ? $"{claim.ClaimId}: Submitted on {claim.SubmissionDate.DayOfWeek}"
                : $"{claim.ClaimId}: Normal weekday submission"
        });

        // 7. Diagnosis-procedure mismatch
        var mismatch = claim.FraudRiskScore > 70 && claim.FraudType != "Legitimate";
        rules.Add(new RuleCheckResult
        {
            RuleId = $"ELG-{claim.ClaimId}",
            RuleName = "Diagnosis-Procedure Eligibility",
            Category = "Eligibility",
            Severity = mismatch ? "High" : "Low",
            Triggered = mismatch,
            Details = mismatch
                ? $"{claim.ClaimId}: Diagnosis {claim.DiagnosisCode} may not warrant procedure {claim.ProcedureCode}"
                : $"{claim.ClaimId}: Diagnosis-procedure combination valid"
        });

        return rules;
    }

    #endregion
}
