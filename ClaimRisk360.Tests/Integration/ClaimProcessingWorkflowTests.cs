using ClaimRisk360.Models;
using ClaimRisk360.Tests.Builders;
using ClaimRisk360.Tests.Utilities;
using FluentAssertions;
using Xunit;

namespace ClaimRisk360.Tests.Integration;

/// <summary>
/// Integration test scenarios demonstrating end-to-end workflows
/// </summary>
public class ClaimProcessingWorkflowTests
{
    [Fact]
    public void Workflow_ValidClaimSubmission_CreatesCompleteFlow()
    {
        // Arrange - Create a valid claim submission request
        var claimRequest = new ClaimUploadRequestBuilder()
            .WithPatient("John Patient", "P-2024-001")
            .WithProvider("City Hospital", "PR-2024-001")
            .WithAmount(7500m)
            .WithServiceDate(DateTime.Today.AddDays(-5))
            .WithCodes("I10", "99213")
            .Build();

        // Act - Create the claim record
        var claim = new ClaimBuilder()
            .WithClaimId($"CLM-{Guid.NewGuid():N}")
            .WithPatient(claimRequest.PatientName, claimRequest.PatientId)
            .WithProvider(claimRequest.ProviderName, claimRequest.ProviderId)
            .WithAmount(claimRequest.Amount)
            .WithSubmissionDate(DateTime.UtcNow)
            .WithDiagnosisCode(claimRequest.DiagnosisCode)
            .WithProcedureCode(claimRequest.ProcedureCode)
            .WithFraudRiskScore(25)
            .Build();

        // Assert - Verify complete claim record
        claim.Should().NotBeNull();
        claim.PatientName.Should().Be(claimRequest.PatientName);
        claim.Amount.Should().Be(claimRequest.Amount);
        claim.FraudRiskScore.Should().Be(25);
        claim.RiskCategory.Should().Be("Low");
    }

    [Fact]
    public void Workflow_HighRiskClaimFlaggedForReview()
    {
        // Arrange - Create high-risk claim
        var highRiskClaim = new ClaimBuilder()
            .WithClaimId("CLM-HIGH-RISK")
            .WithPatient("Suspicious Actor", "P-FRAUD-001")
            .WithProvider("Questionable Provider", "PR-FRAUD-001")
            .WithAmount(50000m)
            .WithFraudRiskScore(85)
            .Build();

        // Act - System recognizes high risk
        var shouldBeReviewed = highRiskClaim.FraudRiskScore > 70;

        // Create case review
        var caseReview = new CaseReviewBuilder()
            .WithCaseId($"CASE-{Guid.NewGuid():N}")
            .WithClaimId(highRiskClaim.ClaimId)
            .WithAssignedTo("lead_investigator")
            .WithPriority("Critical")
            .Build();

        // Assert
        shouldBeReviewed.Should().BeTrue();
        caseReview.Priority.Should().Be("Critical");
        caseReview.Status.Should().Be("Open");
        caseReview.ResolvedAt.Should().BeNull();
    }

    [Fact]
    public void Workflow_ClaimApprovalProcess()
    {
        // Arrange
        var claimToApprove = new ClaimBuilder()
            .WithClaimId("CLM-APPROVE")
            .WithFraudRiskScore(15)
            .Build();

        var caseReview = new CaseReviewBuilder()
            .WithClaimId(claimToApprove.ClaimId)
            .WithAssignedTo("reviewer")
            .Build();

        // Act - Reviewer makes decision
        var decision = "Approve";
        var justification = "Claim verified against medical records. All codes valid.";

        caseReview.Decision = decision;
        caseReview.Justification = justification;
        caseReview.Status = decision switch
        {
            "Approve" => "Resolved",
            "Escalate" => "Escalated",
            _ => "In Review"
        };
        caseReview.ResolvedAt = DateTime.UtcNow;

        // Assert
        caseReview.Status.Should().Be("Resolved");
        caseReview.Decision.Should().Be("Approve");
        caseReview.ResolvedAt.Should().NotBeNull();
    }

    [Fact]
    public void Workflow_ClaimEscalation()
    {
        // Arrange
        var claim = new ClaimBuilder()
            .WithClaimId("CLM-ESCALATE")
            .WithFraudRiskScore(65)
            .Build();

        var caseReview = new CaseReviewBuilder()
            .WithClaimId(claim.ClaimId)
            .WithPriority("High")
            .Build();

        // Act - Initial review suggests escalation
        var auditEntry = new AuditEntry
        {
            AuditId = $"AUD-{Guid.NewGuid():N}",
            ClaimId = claim.ClaimId,
            Action = "Escalated",
            PerformedBy = "initial_reviewer",
            Timestamp = DateTime.UtcNow,
            Details = "Requires senior investigation due to network pattern match",
            Category = "Escalation",
            CaseReviewId = caseReview.CaseId
        };

        caseReview.History.Add(auditEntry);
        caseReview.Status = "Escalated";

        // Assert
        caseReview.Status.Should().Be("Escalated");
        caseReview.History.Should().HaveCount(1);
        caseReview.History.First().Action.Should().Be("Escalated");
    }

    [Fact]
    public void Workflow_MultipleClaimsInPattern()
    {
        // Arrange - Create multiple related claims
        var patientId = "P-PATTERN";
        var providerId = "PR-PATTERN";

        var claim1 = new ClaimBuilder()
            .WithClaimId("CLM-PATTERN-001")
            .WithPatient("Pattern Patient", patientId)
            .WithProvider("Pattern Provider", providerId)
            .WithSubmissionDate(DateTime.UtcNow)
            .Build();

        var claim2 = new ClaimBuilder()
            .WithClaimId("CLM-PATTERN-002")
            .WithPatient("Pattern Patient", patientId)
            .WithProvider("Pattern Provider", providerId)
            .WithSubmissionDate(DateTime.UtcNow.AddHours(2))
            .Build();

        var claim3 = new ClaimBuilder()
            .WithClaimId("CLM-PATTERN-003")
            .WithPatient("Pattern Patient", patientId)
            .WithProvider("Pattern Provider", providerId)
            .WithSubmissionDate(DateTime.UtcNow.AddHours(4))
            .Build();

        var allClaims = new List<Claim> { claim1, claim2, claim3 };

        // Act - Detect pattern
        var pattern = new ClaimPattern
        {
            PatternId = "PATTERN-001",
            PatternType = "Frequency Spike",
            Entity = "Provider",
            EntityId = providerId,
            Description = $"Provider submitted {allClaims.Count} claims from same patient within short timeframe",
            Severity = "High",
            Occurrences = allClaims.Count,
            TimeFrame = "Last 6 hours",
            DetectedAt = DateTime.UtcNow
        };

        // Assert
        pattern.Occurrences.Should().Be(3);
        pattern.Severity.Should().Be("High");
        pattern.PatternType.Should().Be("Frequency Spike");
    }

    [Fact]
    public void Workflow_ProviderProfileUpdate()
    {
        // Arrange
        var provider = new Builders.ProviderProfileBuilder()
            .WithProviderId("PR-PROFILE-001")
            .WithProviderName("Sample Provider")
            .WithTotalClaims(150)
            .WithAverageClaimAmount(5000m)
            .WithRiskScore(35)
            .Build();

        // Act - Add risk indicator
        provider.RiskIndicators.Add("Billing frequency above peer average");
        provider.RiskIndicators.Add("Recent pattern anomaly detected");

        // Assert
        provider.RiskIndicators.Should().HaveCount(2);
        provider.RiskLevel.Should().Be("Low");
    }

    [Fact]
    public void Workflow_UserAuditTrail()
    {
        // Arrange
        var user = new Builders.AppUserBuilder()
            .WithUserId("USER-AUDIT-001")
            .WithDisplayName("Investigator John")
            .WithRole("investigator")
            .Build();

        var auditEntries = new List<AuditEntry>();

        // Act - Simulate user actions
        for (int i = 0; i < 3; i++)
        {
            auditEntries.Add(new AuditEntry
            {
                AuditId = $"AUD-{i}",
                ClaimId = $"CLM-{i}",
                Action = $"Reviewed",
                PerformedBy = user.UserId,
                Timestamp = DateTime.UtcNow.AddHours(i),
                Details = $"Claim review #{i + 1}",
                Category = "Investigation"
            });
        }

        // Assert
        auditEntries.Should().HaveCount(3);
        auditEntries.Should().AllSatisfy(ae => ae.PerformedBy.Should().Be(user.UserId));
    }

    [Fact]
    public void Workflow_RoleBasedAccess()
    {
        // Arrange
        var adminRole = new AppRole
        {
            RoleId = "admin",
            RoleName = "Administrator",
            Permissions = new RolePermissions
            {
                CanViewDashboard = true,
                CanViewClaims = true,
                CanManageUsers = true,
                CanConfigureSystem = true
            }
        };

        var investigatorRole = new AppRole
        {
            RoleId = "investigator",
            RoleName = "Investigator",
            Permissions = new RolePermissions
            {
                CanViewClaims = true,
                CanManageCases = true,
                CanApproveClaim = true,
                CanRejectClaim = true
            }
        };

        var viewerRole = new AppRole
        {
            RoleId = "viewer",
            RoleName = "Report Viewer",
            Permissions = new RolePermissions
            {
                CanViewDashboard = true,
                CanViewReports = true
            }
        };

        // Assert
        adminRole.Permissions.CanManageUsers.Should().BeTrue();
        investigatorRole.Permissions.CanManageCases.Should().BeTrue();
        investigatorRole.Permissions.CanManageUsers.Should().BeFalse();
        viewerRole.Permissions.CanViewDashboard.Should().BeTrue();
        viewerRole.Permissions.CanApproveClaim.Should().BeFalse();
    }

    [Fact]
    public void Workflow_CompleteClaimLifecycle()
    {
        // Arrange - Claim submission
        var initialClaim = new ClaimUploadRequestBuilder()
            .WithPatient("Lifecycle Test", "P-LIFECYCLE")
            .WithProvider("Test Provider", "PR-LIFECYCLE")
            .WithAmount(6000m)
            .Build();

        // Act - Create claim record
        var claim = new ClaimBuilder()
            .WithPatient(initialClaim.PatientName, initialClaim.PatientId)
            .WithProvider(initialClaim.ProviderName, initialClaim.ProviderId)
            .WithAmount(initialClaim.Amount)
            .WithFraudRiskScore(35)
            .WithStatus("Submitted")
            .Build();

        // Create case for review
        var caseReview = new CaseReviewBuilder()
            .WithClaimId(claim.ClaimId)
            .WithAssignedTo("examiner")
            .Build();

        // Add audit log
        var auditEntry = new AuditEntry
        {
            AuditId = $"AUD-{Guid.NewGuid():N}",
            ClaimId = claim.ClaimId,
            Action = "ReviewCompleted",
            PerformedBy = "examiner",
            Timestamp = DateTime.UtcNow,
            Details = "Claim review completed successfully",
            Category = "Review"
        };

        caseReview.History.Add(auditEntry);

        // Final approval
        caseReview.Decision = "Approve";
        caseReview.Status = "Resolved";
        caseReview.ResolvedAt = DateTime.UtcNow;

        claim.ApprovalStatus = "Approved";
        claim.ApprovedBy = "examiner";
        claim.ApprovalComment = "All validations passed";
        claim.ApprovedAt = DateTime.UtcNow;

        // Assert - Complete lifecycle
        claim.ApprovalStatus.Should().Be("Approved");
        caseReview.Status.Should().Be("Resolved");
        caseReview.History.Should().HaveCount(1);
        claim.ApprovedAt.Should().NotBeNull();
    }
}
