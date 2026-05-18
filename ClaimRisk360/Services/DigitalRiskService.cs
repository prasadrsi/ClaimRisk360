using ClaimRisk360.Data;
using ClaimRisk360.Models;

namespace ClaimRisk360.Services;

public class DigitalRiskService
{
    private readonly AppDbContext _db;

    public DigitalRiskService(AppDbContext db)
    {
        _db = db;
    }

    public List<DigitalRiskSignal> GetAllSignals() =>
        _db.DigitalRiskSignals.OrderByDescending(s => s.DetectedAt).ToList();

    public List<DigitalRiskSignal> GetByClaimId(string claimId) =>
        _db.DigitalRiskSignals.Where(s => s.ClaimId == claimId).OrderByDescending(s => s.DetectedAt).ToList();

    public List<DigitalRiskSignal> GetByRiskLevel(string level) =>
        _db.DigitalRiskSignals.Where(s => s.RiskLevel == level).OrderByDescending(s => s.DetectedAt).ToList();

    public List<StpDecision> GetStpDecisions() =>
        _db.StpDecisions.OrderByDescending(s => s.ProcessedAt).ToList();

    public static void SeedDigitalData(AppDbContext db)
    {
        if (db.DigitalRiskSignals.Any()) return;

        var claims = db.Claims.ToList();
        var counter = 0;
        string[] vpnIps = ["185.220.101.42", "104.244.76.13", "198.98.56.78"];
        string[] normalIps = ["72.134.89.201", "98.45.167.33", "104.28.215.90", "68.192.44.17"];
        string[] devices = ["DEV-A1B2C3", "DEV-D4E5F6", "DEV-G7H8I9", "DEV-J0K1L2", "DEV-M3N4O5"];
        string[] agents = [
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/120.0",
            "Mozilla/5.0 (iPhone; CPU iPhone OS 17_2) Safari/605.1",
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 14_2) Firefox/121.0",
            "python-requests/2.31.0",
            "curl/8.4.0"
        ];

        var signals = new List<DigitalRiskSignal>();

        foreach (var claim in claims.Take(25))
        {
            var isHighRisk = claim.FraudRiskScore > 70;
            var isMedRisk = claim.FraudRiskScore > 50;
            var deviceIdx = Random.Shared.Next(devices.Length);
            var reuseCount = claims.Count(c => c.PatientId != claim.PatientId && Random.Shared.NextDouble() < 0.15);

            if (isHighRisk && reuseCount > 0)
                signals.Add(new DigitalRiskSignal { SignalId = $"DRS-{++counter:D5}", ClaimId = claim.ClaimId, SignalType = "Device Reuse", DeviceId = devices[deviceIdx], IpAddress = normalIps[Random.Shared.Next(normalIps.Length)], GeoLocation = claim.Location, UserAgent = agents[Random.Shared.Next(3)], RiskLevel = "High", Details = $"Device {devices[deviceIdx]} used by {reuseCount + 1} different claimants", DetectedAt = claim.SubmissionDate.AddHours(Random.Shared.Next(1, 12)) });

            if (isHighRisk && Random.Shared.NextDouble() < 0.4)
                signals.Add(new DigitalRiskSignal { SignalId = $"DRS-{++counter:D5}", ClaimId = claim.ClaimId, SignalType = "VPN/Proxy", DeviceId = devices[deviceIdx], IpAddress = vpnIps[Random.Shared.Next(vpnIps.Length)], GeoLocation = "Unknown (TOR/VPN)", UserAgent = agents[Random.Shared.Next(agents.Length)], RiskLevel = "Critical", Details = "Submission originated from known VPN/proxy IP address", DetectedAt = claim.SubmissionDate.AddHours(Random.Shared.Next(1, 6)) });

            if (isMedRisk && Random.Shared.NextDouble() < 0.3)
                signals.Add(new DigitalRiskSignal { SignalId = $"DRS-{++counter:D5}", ClaimId = claim.ClaimId, SignalType = "Geo Mismatch", DeviceId = devices[deviceIdx], IpAddress = normalIps[Random.Shared.Next(normalIps.Length)], GeoLocation = "Foreign IP: Romania", UserAgent = agents[Random.Shared.Next(3)], RiskLevel = "High", Details = $"IP geolocation does not match claim location ({claim.Location})", DetectedAt = claim.SubmissionDate.AddHours(Random.Shared.Next(1, 8)) });

            if (isHighRisk && Random.Shared.NextDouble() < 0.35)
                signals.Add(new DigitalRiskSignal { SignalId = $"DRS-{++counter:D5}", ClaimId = claim.ClaimId, SignalType = "Rapid Submission", DeviceId = devices[deviceIdx], IpAddress = normalIps[Random.Shared.Next(normalIps.Length)], GeoLocation = claim.Location, UserAgent = agents[Random.Shared.Next(agents.Length)], RiskLevel = "Medium", Details = "Multiple claims submitted within 2 minutes from same session", DetectedAt = claim.SubmissionDate.AddMinutes(Random.Shared.Next(1, 30)) });

            if (claim.FraudType == "Collusion" && Random.Shared.NextDouble() < 0.5)
                signals.Add(new DigitalRiskSignal { SignalId = $"DRS-{++counter:D5}", ClaimId = claim.ClaimId, SignalType = "Bot Pattern", DeviceId = devices[deviceIdx], IpAddress = normalIps[Random.Shared.Next(normalIps.Length)], GeoLocation = claim.Location, UserAgent = agents[3 + Random.Shared.Next(2)], RiskLevel = "Critical", Details = "Non-browser user agent detected — possible automated submission", DetectedAt = claim.SubmissionDate.AddHours(Random.Shared.Next(1, 4)) });
        }

        db.DigitalRiskSignals.AddRange(signals);
        db.SaveChanges();

        // STP Decisions
        foreach (var claim in claims)
        {
            var digitalFlags = signals.Count(s => s.ClaimId == claim.ClaimId && s.RiskLevel is "Critical" or "High");
            string action, reason;

            if (claim.FraudRiskScore <= 25 && digitalFlags == 0)
            { action = "Auto-Approved"; reason = "Low risk score, no rule violations, clean digital signals"; }
            else if (claim.FraudRiskScore >= 85 || digitalFlags >= 2)
            { action = "Auto-Rejected"; reason = claim.FraudRiskScore >= 85 ? "Risk score exceeds auto-reject threshold (85)" : $"Multiple critical digital risk signals ({digitalFlags})"; }
            else
            { action = "Routed to Review"; reason = "Risk score in grey zone — requires human investigation"; }

            db.StpDecisions.Add(new StpDecision
            {
                ClaimId = claim.ClaimId, Action = action, Reason = reason, RiskScore = claim.FraudRiskScore,
                RulesFired = claim.FraudRiskScore > 50 ? Random.Shared.Next(1, 4) : 0,
                DigitalRiskFlags = digitalFlags, ProcessedAt = claim.SubmissionDate.AddSeconds(Random.Shared.Next(2, 30))
            });
        }

        db.SaveChanges();
    }
}
