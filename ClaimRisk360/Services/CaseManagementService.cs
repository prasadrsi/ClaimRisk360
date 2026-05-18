using ClaimRisk360.Data;
using ClaimRisk360.Models;
using Microsoft.EntityFrameworkCore;

namespace ClaimRisk360.Services;

public class CaseManagementService
{
    private readonly AppDbContext _db;
    private readonly AuditService _auditService;
    private readonly ClaimApprovalService _approvalService;
    private readonly NotificationService _notificationService;

    public CaseManagementService(AppDbContext db, AuditService auditService,
        ClaimApprovalService approvalService, NotificationService notificationService)
    {
        _db = db;
        _auditService = auditService;
        _approvalService = approvalService;
        _notificationService = notificationService;
    }

    public List<CaseReview> GetAll() =>
        _db.CaseReviews.Include(c => c.History).OrderByDescending(c => c.CreatedAt).ToList();

    public CaseReview? GetCase(string caseId) =>
        _db.CaseReviews.Include(c => c.History).FirstOrDefault(c => c.CaseId == caseId);

    public CaseReview? GetCaseByClaimId(string claimId) =>
        _db.CaseReviews.Include(c => c.History).FirstOrDefault(c => c.ClaimId == claimId);

    public string? UpdateDecision(string caseId, string decision, string justification, string userName)
    {
        if (string.IsNullOrWhiteSpace(justification))
            return "Justification comment is mandatory.";

        var caseReview = GetCase(caseId);
        if (caseReview is null) return "Case not found.";

        caseReview.Decision = decision;
        caseReview.Justification = justification;
        caseReview.Status = decision switch
        {
            "Approve" => "Resolved",
            "Escalate" => "Escalated",
            "Monitor" => "In Review",
            _ => caseReview.Status
        };

        if (decision == "Approve")
        {
            caseReview.ResolvedAt = DateTime.UtcNow;
            _approvalService.ApproveClaim(caseReview.ClaimId, justification, userName);
        }

        caseReview.History.Add(new AuditEntry
        {
            AuditId = $"AUD-CASE-{Random.Shared.Next(10000, 99999)}",
            ClaimId = caseReview.ClaimId,
            Action = $"Decision: {decision}",
            PerformedBy = userName,
            Timestamp = DateTime.UtcNow,
            Details = justification,
            Category = "Case Management",
            CaseReviewId = caseReview.CaseId
        });

        _db.SaveChanges();

        _auditService.Log(caseReview.ClaimId, $"Case Decision: {decision}", userName, justification, "Case Management");

        _ = _notificationService.SendNotification(
            $"Case {decision}",
            $"{caseId} ({caseReview.ClaimId}) — {decision} by {userName}",
            decision == "Approve" ? "success" : decision == "Escalate" ? "warning" : "info",
            caseReview.ClaimId);
        _ = _notificationService.SendDataRefresh("cases", caseId);

        return null;
    }

    public static void SeedCases(AppDbContext db)
    {
        if (db.CaseReviews.Any()) return;

        var flaggedClaims = db.Claims.Where(c => c.FraudRiskScore > 50).Take(12).ToList();
        var investigators = new[] { "Sarah Chen", "James Rivera", "Priya Sharma", "Marcus Johnson" };
        int counter = 0;

        foreach (var claim in flaggedClaims)
        {
            var isResolved = Random.Shared.NextDouble() > 0.5;
            var daysAgo = Random.Shared.Next(5, 40);

            var caseReview = new CaseReview
            {
                CaseId = $"CASE-{++counter:D4}",
                ClaimId = claim.ClaimId,
                AssignedTo = investigators[Random.Shared.Next(investigators.Length)],
                Priority = claim.FraudRiskScore switch
                {
                    > 85 => "Critical",
                    > 70 => "High",
                    > 50 => "Medium",
                    _ => "Low"
                },
                CreatedAt = DateTime.UtcNow.AddDays(-daysAgo),
                Status = isResolved ? "Resolved" : (claim.FraudRiskScore > 80 ? "Escalated" : "In Review"),
                Decision = isResolved ? "Approve" : "",
                Justification = isResolved ? "Reviewed and confirmed legitimate after investigation" : "",
                ResolvedAt = isResolved ? DateTime.UtcNow.AddDays(-daysAgo + 3) : null,
            };

            caseReview.History.Add(new AuditEntry
            {
                AuditId = $"AUD-CASE-{Random.Shared.Next(10000, 99999)}",
                ClaimId = claim.ClaimId,
                Action = "Case Created",
                PerformedBy = "System",
                Timestamp = caseReview.CreatedAt,
                Details = $"Auto-created from fraud score {claim.FraudRiskScore}",
                Category = "Case Management",
                CaseReviewId = caseReview.CaseId
            });

            db.CaseReviews.Add(caseReview);
        }

        db.SaveChanges();
    }
}
