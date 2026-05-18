using ClaimRisk360.Models;

namespace ClaimRisk360.Services;

public class PatternAnalysisService
{
    private readonly FraudDetectionService _fraudService;

    public PatternAnalysisService(FraudDetectionService fraudService)
    {
        _fraudService = fraudService;
    }

    #region Synchronous Methods (Legacy)
    public List<ClaimPattern> DetectPatterns()
    {
        var claims = _fraudService.GetAllClaims();
        return AnalyzePatterns(claims);
    }
    #endregion

    #region Async Methods (New - Performance Optimized)

    /// <summary>
    /// Detect patterns asynchronously
    /// </summary>
    public async Task<List<ClaimPattern>> DetectPatternsAsync()
    {
        var claims = await _fraudService.GetAllClaimsAsync();
        return AnalyzePatterns(claims);
    }

    #endregion

    private List<ClaimPattern> AnalyzePatterns(List<Claim> claims)
    {
        var patterns = new List<ClaimPattern>();
        var counter = 0;

        // 1. Frequency spikes by patient
        foreach (var group in claims.GroupBy(c => c.PatientId))
        {
            if (group.Count() >= 4)
            {
                var patient = group.First();
                var daySpan = (group.Max(c => c.SubmissionDate) - group.Min(c => c.SubmissionDate)).TotalDays;
                patterns.Add(new ClaimPattern
                {
                    PatternId = $"PAT-{++counter:D4}",
                    PatternType = "Frequency Spike",
                    Entity = patient.PatientName,
                    EntityId = patient.PatientId,
                    Description = $"{group.Count()} claims submitted within {daySpan:F0} days",
                    Severity = group.Count() >= 6 ? "Critical" : "High",
                    Occurrences = group.Count(),
                    TimeFrame = $"Last {daySpan:F0} days",
                    DetectedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 15))
                });
            }
        }

        // 2. Provider frequency spikes
        foreach (var group in claims.GroupBy(c => c.ProviderId))
        {
            if (group.Count() >= 6)
            {
                var provider = group.First();
                patterns.Add(new ClaimPattern
                {
                    PatternId = $"PAT-{++counter:D4}",
                    PatternType = "Frequency Spike",
                    Entity = provider.ProviderName,
                    EntityId = provider.ProviderId,
                    Description = $"Provider submitted {group.Count()} claims – unusually high volume",
                    Severity = group.Count() >= 10 ? "Critical" : "High",
                    Occurrences = group.Count(),
                    TimeFrame = "Last 90 days",
                    DetectedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 10))
                });
            }
        }

        // 3. Amount anomalies
        var avgAmount = claims.Average(c => c.Amount);
        foreach (var claim in claims.Where(c => c.Amount > avgAmount * 2.5m))
        {
            patterns.Add(new ClaimPattern
            {
                PatternId = $"PAT-{++counter:D4}",
                PatternType = "Amount Anomaly",
                Entity = claim.ProviderName,
                EntityId = claim.ClaimId,
                Description = $"${claim.Amount:N2} is {claim.Amount / avgAmount:F1}x the average (${avgAmount:N2})",
                Severity = claim.Amount > avgAmount * 4 ? "Critical" : "High",
                Occurrences = 1,
                TimeFrame = claim.SubmissionDate.ToString("MMM dd, yyyy"),
                DetectedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 20))
            });
        }

        // 4. Timing anomalies (weekend claims)
        var weekendClaims = claims.Where(c => c.SubmissionDate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday).ToList();
        if (weekendClaims.Count > 3)
        {
            patterns.Add(new ClaimPattern
            {
                PatternId = $"PAT-{++counter:D4}",
                PatternType = "Timing Anomaly",
                Entity = "System-wide",
                EntityId = "ALL",
                Description = $"{weekendClaims.Count} claims submitted on weekends – potential off-hours submission pattern",
                Severity = "Medium",
                Occurrences = weekendClaims.Count,
                TimeFrame = "Last 90 days",
                DetectedAt = DateTime.UtcNow.AddDays(-3)
            });
        }

        // 5. Geographic anomalies
        foreach (var group in claims.GroupBy(c => c.PatientId))
        {
            var locations = group.Select(c => c.Location).Distinct().ToList();
            if (locations.Count >= 3)
            {
                var patient = group.First();
                patterns.Add(new ClaimPattern
                {
                    PatternId = $"PAT-{++counter:D4}",
                    PatternType = "Geographic Anomaly",
                    Entity = patient.PatientName,
                    EntityId = patient.PatientId,
                    Description = $"Claims from {locations.Count} different locations: {string.Join(", ", locations)}",
                    Severity = "High",
                    Occurrences = locations.Count,
                    TimeFrame = "Last 90 days",
                    DetectedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 12))
                });
            }
        }

        // 6. Near-policy-limit claims (simulated)
        foreach (var claim in claims.Where(c => c.Amount > 12000))
        {
            patterns.Add(new ClaimPattern
            {
                PatternId = $"PAT-{++counter:D4}",
                PatternType = "Behavioral",
                Entity = claim.PatientName,
                EntityId = claim.ClaimId,
                Description = $"Claim of ${claim.Amount:N2} is near policy limit threshold",
                Severity = "Medium",
                Occurrences = 1,
                TimeFrame = claim.SubmissionDate.ToString("MMM dd, yyyy"),
                DetectedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 15))
            });
        }

        return patterns.OrderByDescending(p => p.Severity == "Critical" ? 4 : p.Severity == "High" ? 3 : p.Severity == "Medium" ? 2 : 1)
            .ThenByDescending(p => p.DetectedAt)
            .ToList();
    }
}
