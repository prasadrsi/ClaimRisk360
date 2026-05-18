using ClaimRisk360.Models;
using FluentAssertions;
using Xunit;

namespace ClaimRisk360.Tests.Utilities;

public class TestDataBuildersTests
{
    [Fact]
    public void ClaimBuilder_BuildsValidClaim()
    {
        // Arrange & Act
        var claim = new Builders.ClaimBuilder()
            .WithClaimId("TEST-001")
            .WithPatient("John Doe", "P123")
            .WithProvider("Dr. Smith", "PR456")
            .WithAmount(7500)
            .WithFraudRiskScore(45)
            .Build();

        // Assert
        claim.ClaimId.Should().Be("TEST-001");
        claim.PatientName.Should().Be("John Doe");
        claim.Amount.Should().Be(7500);
        claim.FraudRiskScore.Should().Be(45);
    }

    [Fact]
    public void ClaimBuilder_FluentChaining_Works()
    {
        // Arrange & Act
        var claim = new Builders.ClaimBuilder()
            .WithClaimId("CHAIN-001")
            .WithPatient("Test", "P1")
            .WithProvider("Provider", "PR1")
            .WithAmount(5000)
            .WithStatus("Approved")
            .WithApprovalStatus("Approved")
            .Build();

        // Assert
        claim.Status.Should().Be("Approved");
        claim.ApprovalStatus.Should().Be("Approved");
    }

    [Fact]
    public void ClaimUploadRequestBuilder_BuildsValidRequest()
    {
        // Arrange & Act
        var request = new Builders.ClaimUploadRequestBuilder()
            .WithPatient("Jane Smith", "P789")
            .WithProvider("Hospital XYZ", "PR999")
            .WithAmount(8500)
            .WithCodes("I10", "93000")
            .Build();

        // Assert
        request.PatientName.Should().Be("Jane Smith");
        request.Amount.Should().Be(8500);
        request.DiagnosisCode.Should().Be("I10");
        request.ProcedureCode.Should().Be("93000");
    }

    [Fact]
    public void CaseReviewBuilder_BuildsValidCase()
    {
        // Arrange & Act
        var caseReview = new Builders.CaseReviewBuilder()
            .WithCaseId("CASE-001")
            .WithClaimId("CLAIM-001")
            .WithAssignedTo("investigator1")
            .WithPriority("High")
            .Build();

        // Assert
        caseReview.CaseId.Should().Be("CASE-001");
        caseReview.Priority.Should().Be("High");
        caseReview.Status.Should().Be("Open");
    }

    [Fact]
    public void CaseReviewBuilder_AsResolved_SetsResolvedAt()
    {
        // Arrange & Act
        var caseReview = new Builders.CaseReviewBuilder()
            .AsResolved()
            .Build();

        // Assert
        caseReview.Status.Should().Be("Resolved");
        caseReview.ResolvedAt.Should().NotBeNull();
    }

    [Fact]
    public void AppUserBuilder_BuildsValidUser()
    {
        // Arrange & Act
        var user = new Builders.AppUserBuilder()
            .WithUserId("admin-001")
            .WithDisplayName("Admin User")
            .WithEmail("admin@example.com")
            .WithRole("admin")
            .Build();

        // Assert
        user.UserId.Should().Be("admin-001");
        user.DisplayName.Should().Be("Admin User");
        user.RoleId.Should().Be("admin");
    }

    [Fact]
    public void AppUserBuilder_AsInactive_SetIsActiveFalse()
    {
        // Arrange & Act
        var user = new Builders.AppUserBuilder()
            .AsInactive()
            .Build();

        // Assert
        user.IsActive.Should().BeFalse();
    }

    [Fact]
    public void ProviderProfileBuilder_BuildsValidProfile()
    {
        // Arrange & Act
        var profile = new Builders.ProviderProfileBuilder()
            .WithProviderId("PR-HIGH-RISK")
            .WithProviderName("High Risk Provider")
            .WithRiskScore(80)
            .WithTotalClaims(500)
            .Build();

        // Assert
        profile.ProviderId.Should().Be("PR-HIGH-RISK");
        profile.RiskScore.Should().Be(80);
        profile.RiskLevel.Should().Be("High");
    }

    [Theory]
    [InlineData(30, "Low")]
    [InlineData(55, "Medium")]
    [InlineData(85, "High")]
    public void ProviderProfileBuilder_RiskLevelMapping_IsCorrect(int riskScore, string expectedLevel)
    {
        // Arrange & Act
        var profile = new Builders.ProviderProfileBuilder()
            .WithRiskScore(riskScore)
            .Build();

        // Assert
        profile.RiskLevel.Should().Be(expectedLevel);
    }
}

/// <summary>
/// Test utilities for common assertion patterns
/// </summary>
public static class TestAssertions
{
    public static void AssertValidClaim(Claim claim)
    {
        claim.Should().NotBeNull();
        claim.ClaimId.Should().NotBeNullOrEmpty();
        claim.PatientId.Should().NotBeNullOrEmpty();
        claim.ProviderId.Should().NotBeNullOrEmpty();
        claim.Amount.Should().BeGreaterThan(0);
    }

    public static void AssertValidCaseReview(CaseReview caseReview)
    {
        caseReview.Should().NotBeNull();
        caseReview.CaseId.Should().NotBeNullOrEmpty();
        caseReview.ClaimId.Should().NotBeNullOrEmpty();
        caseReview.Status.Should().NotBeNullOrEmpty();
    }

    public static void AssertValidRuleResult(RuleCheckResult result)
    {
        result.Should().NotBeNull();
        result.RuleId.Should().NotBeNullOrEmpty();
        result.RuleName.Should().NotBeNullOrEmpty();
        result.Severity.Should().BeOneOf("Critical", "High", "Medium", "Low", "Info");
    }

    public static void AssertValidationResult(ValidationResult result)
    {
        result.Should().NotBeNull();
        result.Status.Should().BeOneOf("Passed", "Rejected");
        result.Errors.Should().NotBeNull();
        result.Warnings.Should().NotBeNull();
    }
}

/// <summary>
/// Common test data constants
/// </summary>
public static class TestConstants
{
    public const string DefaultClaimId = "CLAIM-TEST-001";
    public const string DefaultPatientId = "P-TEST-001";
    public const string DefaultProviderId = "PR-TEST-001";
    public const string DefaultUserId = "USER-TEST-001";
    public const string DefaultCaseId = "CASE-TEST-001";

    public const decimal HighAmountThreshold = 10000m;
    public const decimal LowAmount = 1000m;
    public const decimal MediumAmount = 5000m;
    public const decimal HighAmount = 25000m;

    public const int LowRiskScore = 20;
    public const int MediumRiskScore = 50;
    public const int HighRiskScore = 80;

    public static readonly string[] ValidDiagnosisCodes = { "I10", "I11", "I12", "I13" };
    public static readonly string[] ValidProcedureCodes = { "93000", "93005", "99213", "99214" };
    public static readonly string[] ActiveProviders = { "PR-TEST-001", "PR-TEST-002", "PR-TEST-003" };

    public static DateTime GetStandardServiceDate() => DateTime.Today.AddDays(-10);
    public static DateTime GetRecentServiceDate() => DateTime.Today.AddDays(-1);
    public static DateTime GetOldServiceDate() => DateTime.Today.AddYears(-2);
}
