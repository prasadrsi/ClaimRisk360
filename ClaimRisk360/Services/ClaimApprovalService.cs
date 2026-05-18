using ClaimRisk360.Data;
using ClaimRisk360.Models;

namespace ClaimRisk360.Services;

/// <summary>
/// Business Logic: claim approval workflow.
/// Handles both auto-approval (STP rules) and manual approval (requires mandatory comment).
/// </summary>
public class ClaimApprovalService
{
    private readonly ClaimRepository _claimRepo;
    private readonly AuditService _auditService;
    private readonly NotificationService _notificationService;

    public ClaimApprovalService(ClaimRepository claimRepo, AuditService auditService, NotificationService notificationService)
    {
        _claimRepo = claimRepo;
        _auditService = auditService;
        _notificationService = notificationService;
    }

    /// <summary>
    /// Apply auto-approvals to all pending claims (called once at startup via seeder).
    /// </summary>
    public void ApplyAutoApprovals()
    {
        foreach (var claim in _claimRepo.GetAllClaims())
        {
            if (claim.FraudRiskScore <= 25 && claim.FraudType == "Legitimate")
            {
                claim.ApprovalStatus = "Auto-Approved";
                claim.ApprovalMethod = "STP Rule";
                claim.ApprovedBy = "System";
                claim.ApprovalComment = "Auto-approved: risk score ? 25, no fraud indicators, within normal parameters";
                claim.ApprovedAt = claim.SubmissionDate.AddSeconds(Random.Shared.Next(2, 30));
                claim.Status = "Approved";
            }
            else if (claim.FraudRiskScore >= 85)
            {
                claim.ApprovalStatus = "Rejected";
                claim.ApprovalMethod = "STP Rule";
                claim.ApprovedBy = "System";
                claim.ApprovalComment = $"Auto-rejected: risk score {claim.FraudRiskScore} exceeds auto-reject threshold (85)";
                claim.ApprovedAt = claim.SubmissionDate.AddSeconds(Random.Shared.Next(2, 30));
                claim.Status = "Rejected";
            }
            else
            {
                claim.ApprovalStatus = "Pending Review";
                claim.ApprovalMethod = string.Empty;
            }
        }
        _claimRepo.SaveChanges();
    }

    /// <summary>
    /// Manually approve a claim. Comment is mandatory.
    /// Returns error message if validation fails, null on success.
    /// </summary>
    public string? ApproveClaim(string claimId, string comment, string approvedBy)
    {
        if (string.IsNullOrWhiteSpace(comment))
            return "Approval comment is mandatory when approving a claim.";

        var claim = _claimRepo.GetClaim(claimId);
        if (claim is null)
            return "Claim not found.";

        if (claim.ApprovalStatus is "Auto-Approved" or "Approved")
            return "Claim is already approved.";

        claim.ApprovalStatus = "Approved";
        claim.ApprovalMethod = "Manual";
        claim.ApprovedBy = approvedBy;
        claim.ApprovalComment = comment;
        claim.ApprovedAt = DateTime.UtcNow;
        claim.Status = "Approved";
        _claimRepo.SaveChanges();

        _auditService.Log(claimId, "Claim Approved", approvedBy,
            $"Manual approval: {comment}", "Decision");

        _ = _notificationService.SendNotification(
            "Claim Approved",
            $"{claimId} approved by {approvedBy}",
            "success", claimId);
        _ = _notificationService.SendDataRefresh("claims", claimId);
        _ = _notificationService.SendBadgeUpdate(GetSummary().PendingReview);

        return null;
    }

    /// <summary>
    /// Manually reject a claim. Comment is mandatory.
    /// </summary>
    public string? RejectClaim(string claimId, string comment, string rejectedBy)
    {
        if (string.IsNullOrWhiteSpace(comment))
            return "Rejection comment is mandatory.";

        var claim = _claimRepo.GetClaim(claimId);
        if (claim is null)
            return "Claim not found.";

        claim.ApprovalStatus = "Rejected";
        claim.ApprovalMethod = "Manual";
        claim.ApprovedBy = rejectedBy;
        claim.ApprovalComment = comment;
        claim.ApprovedAt = DateTime.UtcNow;
        claim.Status = "Rejected";
        _claimRepo.SaveChanges();

        _auditService.Log(claimId, "Claim Rejected", rejectedBy,
            $"Manual rejection: {comment}", "Decision");

        _ = _notificationService.SendNotification(
            "Claim Rejected",
            $"{claimId} rejected by {rejectedBy}",
            "danger", claimId);
        _ = _notificationService.SendDataRefresh("claims", claimId);
        _ = _notificationService.SendBadgeUpdate(GetSummary().PendingReview);

        return null;
    }

    /// <summary>
    /// Get approval summary statistics.
    /// </summary>
    public ApprovalSummary GetSummary()
    {
        var claims = _claimRepo.GetAllClaims();
        return new ApprovalSummary
        {
            TotalClaims = claims.Count,
            AutoApproved = claims.Count(c => c.ApprovalStatus == "Auto-Approved"),
            ManuallyApproved = claims.Count(c => c.ApprovalStatus == "Approved"),
            Rejected = claims.Count(c => c.ApprovalStatus == "Rejected"),
            PendingReview = claims.Count(c => c.ApprovalStatus == "Pending Review")
        };
    }
}

public class ApprovalSummary
{
    public int TotalClaims { get; set; }
    public int AutoApproved { get; set; }
    public int ManuallyApproved { get; set; }
    public int Rejected { get; set; }
    public int PendingReview { get; set; }
}
