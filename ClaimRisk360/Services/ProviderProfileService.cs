using ClaimRisk360.Models;

namespace ClaimRisk360.Services;

public class ProviderProfileService
{
    private readonly FraudDetectionService _fraudService;

    public ProviderProfileService(FraudDetectionService fraudService)
    {
        _fraudService = fraudService;
    }

    public List<ProviderProfile> GetAllProfiles()
    {
        var claims = _fraudService.GetAllClaims();
        var groups = claims.GroupBy(c => c.ProviderId);
        var profiles = new List<ProviderProfile>();

        foreach (var group in groups)
        {
            var first = group.First();
            var totalClaims = group.Count();
            var totalBilled = group.Sum(c => c.Amount);
            var avgAmount = totalClaims > 0 ? totalBilled / totalClaims : 0;
            var flagged = group.Count(c => c.FraudRiskScore > 50);
            var flagRate = totalClaims > 0 ? (double)flagged / totalClaims * 100 : 0;

            // Peer average: all providers in same specialty
            var peerClaims = claims.Where(c => c.Specialty == first.Specialty && c.ProviderId != first.ProviderId);
            var peerAvg = peerClaims.Any() ? peerClaims.Average(c => c.Amount) : avgAmount;
            var deviation = peerAvg > 0 ? (double)((avgAmount - peerAvg) / peerAvg) * 100 : 0;

            var riskScore = CalculateProviderRisk(flagRate, deviation, totalClaims);
            var indicators = new List<string>();
            if (deviation > 40) indicators.Add("Billing significantly above peer average");
            if (flagRate > 40) indicators.Add("High proportion of flagged claims");
            if (totalClaims > 8) indicators.Add("Unusually high claim volume");
            if (group.Any(c => c.FraudType == "Collusion")) indicators.Add("Linked to suspected fraud ring");
            if (group.Select(c => c.PatientId).Distinct().Count() < totalClaims * 0.5)
                indicators.Add("Repeated treatments for same patients");

            profiles.Add(new ProviderProfile
            {
                ProviderId = first.ProviderId,
                ProviderName = first.ProviderName,
                Specialty = first.Specialty,
                Location = first.Location,
                TotalClaims = totalClaims,
                TotalBilled = totalBilled,
                AvgClaimAmount = avgAmount,
                PeerAvgAmount = peerAvg,
                DeviationPercent = Math.Round(deviation, 1),
                FlaggedClaims = flagged,
                FlagRate = Math.Round(flagRate, 1),
                RiskScore = riskScore,
                RiskLevel = riskScore > 70 ? "High" : riskScore > 40 ? "Medium" : "Low",
                RiskIndicators = indicators
            });
        }

        return profiles.OrderByDescending(p => p.RiskScore).ToList();
    }

    public ProviderProfile? GetProfile(string providerId) =>
        GetAllProfiles().FirstOrDefault(p => p.ProviderId == providerId);

    private static int CalculateProviderRisk(double flagRate, double deviation, int volume)
    {
        var score = 0.0;
        score += Math.Min(flagRate, 100) * 0.4;
        score += Math.Min(Math.Abs(deviation), 100) * 0.3;
        score += Math.Min(volume * 3, 30);
        return (int)Math.Min(score, 100);
    }
}
