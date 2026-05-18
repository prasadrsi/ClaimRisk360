using ClaimRisk360.Models;
using FluentAssertions;
using Xunit;

namespace ClaimRisk360.Tests.Models;

public class ClaimTests
{
    [Fact]
    public void Claim_DefaultConstructor_InitializesWithDefaults()
    {
        // Arrange & Act
        var claim = new Claim();

        // Assert
        claim.ClaimId.Should().Be(string.Empty);
        claim.PatientName.Should().Be(string.Empty);
        claim.PatientId.Should().Be(string.Empty);
        claim.ProviderName.Should().Be(string.Empty);
        claim.ProviderId.Should().Be(string.Empty);
        claim.Status.Should().Be("Pending");
        claim.ApprovalStatus.Should().Be("Pending");
        claim.FraudRiskScore.Should().Be(0);
    }

    [Fact]
    public void Claim_SetAllProperties_UpdatesCorrectly()
    {
        // Arrange
        var claim = new Claim();
        var submissionDate = DateTime.UtcNow;

        // Act
        claim.ClaimId = "claim123";
        claim.PatientName = "Jane Doe";
        claim.PatientId = "P456";
        claim.ProviderName = "Hospital ABC";
        claim.ProviderId = "PR789";
        claim.Amount = 5000m;
        claim.SubmissionDate = submissionDate;
        claim.FraudRiskScore = 45;
        claim.Status = "Approved";

        // Assert
        claim.ClaimId.Should().Be("claim123");
        claim.PatientName.Should().Be("Jane Doe");
        claim.Amount.Should().Be(5000m);
        claim.FraudRiskScore.Should().Be(45);
        claim.Status.Should().Be("Approved");
    }

    [Theory]
    [InlineData(15, "Low")]
    [InlineData(30, "Low")]
    [InlineData(45, "Medium")]
    [InlineData(70, "Medium")]
    [InlineData(85, "High")]
    public void Claim_RiskCategory_CalculatedCorrectly(int score, string expectedCategory)
    {
        // Arrange & Act
        var claim = new Claim { FraudRiskScore = score };

        // Assert
        claim.RiskCategory.Should().Be(expectedCategory);
    }

    [Theory]
    [InlineData("Low", "bg-success")]
    [InlineData("Medium", "bg-warning text-dark")]
    [InlineData("High", "bg-danger")]
    public void Claim_RiskBadgeClass_ReturnsCorrectClass(string category, string expectedClass)
    {
        // Arrange
        var claim = new Claim();
        claim.FraudRiskScore = category switch
        {
            "Low" => 20,
            "Medium" => 50,
            _ => 80
        };

        // Act & Assert
        claim.RiskBadgeClass.Should().Be(expectedClass);
    }

    [Fact]
    public void Claim_AddRiskReasons_UpdatesCorrectly()
    {
        // Arrange
        var claim = new Claim();

        // Act
        claim.RiskReasons.Add("Billing frequency spike");
        claim.RiskReasons.Add("Amount exceeds peer average");

        // Assert
        claim.RiskReasons.Should().HaveCount(2);
        claim.RiskReasons.Should().Contain("Billing frequency spike");
    }

    [Theory]
    [InlineData("Auto-Approved", "bg-success")]
    [InlineData("Approved", "bg-success")]
    [InlineData("Rejected", "bg-danger")]
    [InlineData("Pending Review", "bg-warning text-dark")]
    public void Claim_ApprovalBadgeClass_ReturnsCorrectClass(string status, string expectedClass)
    {
        // Arrange & Act
        var claim = new Claim { ApprovalStatus = status };

        // Assert
        claim.ApprovalBadgeClass.Should().Be(expectedClass);
    }

    [Fact]
    public void Claim_SetApprovalDetails_UpdatesCorrectly()
    {
        // Arrange
        var claim = new Claim();
        var approvedAt = DateTime.UtcNow;

        // Act
        claim.ApprovalStatus = "Approved";
        claim.ApprovedBy = "reviewer1";
        claim.ApprovalComment = "Claim verified";
        claim.ApprovedAt = approvedAt;
        claim.ApprovalMethod = "Manual Review";

        // Assert
        claim.ApprovalStatus.Should().Be("Approved");
        claim.ApprovedBy.Should().Be("reviewer1");
        claim.ApprovalComment.Should().Be("Claim verified");
        claim.ApprovedAt.Should().Be(approvedAt);
        claim.ApprovalMethod.Should().Be("Manual Review");
    }
}

public class RuleCheckResultTests
{
    [Fact]
    public void RuleCheckResult_DefaultConstructor_InitializesWithDefaults()
    {
        // Arrange & Act
        var result = new RuleCheckResult();

        // Assert
        result.RuleId.Should().Be(string.Empty);
        result.RuleName.Should().Be(string.Empty);
        result.Category.Should().Be(string.Empty);
        result.Severity.Should().Be("Info");
        result.Triggered.Should().BeFalse();
        result.Details.Should().Be(string.Empty);
    }

    [Fact]
    public void RuleCheckResult_SetAllProperties_UpdatesCorrectly()
    {
        // Arrange
        var result = new RuleCheckResult();

        // Act
        result.RuleId = "rule123";
        result.RuleName = "Duplicate Claim Check";
        result.Category = "Duplicate";
        result.Severity = "Critical";
        result.Triggered = true;
        result.Details = "Duplicate claim found for same patient and procedure";

        // Assert
        result.RuleId.Should().Be("rule123");
        result.RuleName.Should().Be("Duplicate Claim Check");
        result.Category.Should().Be("Duplicate");
        result.Severity.Should().Be("Critical");
        result.Triggered.Should().BeTrue();
        result.Details.Should().Be("Duplicate claim found for same patient and procedure");
    }

    [Theory]
    [InlineData("Critical", "bg-danger")]
    [InlineData("High", "bg-warning text-dark")]
    [InlineData("Medium", "bg-info")]
    [InlineData("Low", "bg-success")]
    public void RuleCheckResult_SeverityBadgeClass_ReturnsCorrectClass(string severity, string expectedClass)
    {
        // Arrange & Act
        var result = new RuleCheckResult { Severity = severity };

        // Assert
        result.SeverityBadgeClass.Should().Be(expectedClass);
    }

    [Theory]
    [InlineData("Duplicate", "bi-files")]
    [InlineData("Threshold", "bi-graph-up-arrow")]
    [InlineData("Blacklist", "bi-slash-circle")]
    [InlineData("Eligibility", "bi-shield-exclamation")]
    [InlineData("Timing", "bi-clock-history")]
    [InlineData("Document", "bi-file-earmark-x")]
    public void RuleCheckResult_IconClass_ReturnsCorrectIcon(string category, string expectedIcon)
    {
        // Arrange & Act
        var result = new RuleCheckResult { Category = category };

        // Assert
        result.IconClass.Should().Be(expectedIcon);
    }
}

public class ProviderProfileTests
{
    [Fact]
    public void ProviderProfile_DefaultConstructor_InitializesWithDefaults()
    {
        // Arrange & Act
        var profile = new ProviderProfile();

        // Assert
        profile.ProviderId.Should().Be(string.Empty);
        profile.ProviderName.Should().Be(string.Empty);
        profile.RiskLevel.Should().Be("Low");
        profile.RiskIndicators.Should().BeEmpty();
    }

    [Fact]
    public void ProviderProfile_SetAllProperties_UpdatesCorrectly()
    {
        // Arrange
        var profile = new ProviderProfile();

        // Act
        profile.ProviderId = "PR123";
        profile.ProviderName = "Dr. Smith Clinic";
        profile.Specialty = "Cardiology";
        profile.Location = "New York";
        profile.TotalClaims = 150;
        profile.TotalBilled = 750000m;
        profile.AvgClaimAmount = 5000m;
        profile.PeerAvgAmount = 4500m;
        profile.DeviationPercent = 11.1;
        profile.FlaggedClaims = 5;
        profile.FlagRate = 3.3;
        profile.RiskScore = 65;
        profile.RiskLevel = "Medium";

        // Assert
        profile.ProviderId.Should().Be("PR123");
        profile.ProviderName.Should().Be("Dr. Smith Clinic");
        profile.RiskScore.Should().Be(65);
        profile.RiskLevel.Should().Be("Medium");
    }

    [Theory]
    [InlineData("High", "bg-danger")]
    [InlineData("Medium", "bg-warning text-dark")]
    [InlineData("Low", "bg-success")]
    public void ProviderProfile_RiskBadgeClass_ReturnsCorrectClass(string riskLevel, string expectedClass)
    {
        // Arrange & Act
        var profile = new ProviderProfile { RiskLevel = riskLevel };

        // Assert
        profile.RiskBadgeClass.Should().Be(expectedClass);
    }

    [Fact]
    public void ProviderProfile_AddRiskIndicators_UpdatesCorrectly()
    {
        // Arrange
        var profile = new ProviderProfile();

        // Act
        profile.RiskIndicators.Add("High billing frequency");
        profile.RiskIndicators.Add("Amount above peer average");
        profile.RiskIndicators.Add("Recent pattern anomaly");

        // Assert
        profile.RiskIndicators.Should().HaveCount(3);
    }
}

public class ClaimPatternTests
{
    [Fact]
    public void ClaimPattern_DefaultConstructor_InitializesWithDefaults()
    {
        // Arrange & Act
        var pattern = new ClaimPattern();

        // Assert
        pattern.PatternId.Should().Be(string.Empty);
        pattern.PatternType.Should().Be(string.Empty);
        pattern.Severity.Should().Be("Medium");
        pattern.TimeFrame.Should().Be(string.Empty);
    }

    [Fact]
    public void ClaimPattern_SetAllProperties_UpdatesCorrectly()
    {
        // Arrange
        var pattern = new ClaimPattern();
        var detectedAt = DateTime.UtcNow;

        // Act
        pattern.PatternId = "pattern123";
        pattern.PatternType = "Frequency Spike";
        pattern.Entity = "Provider";
        pattern.EntityId = "PR456";
        pattern.Description = "Provider submitted 10x normal claims volume";
        pattern.Severity = "High";
        pattern.Occurrences = 15;
        pattern.TimeFrame = "Last 7 days";
        pattern.DetectedAt = detectedAt;

        // Assert
        pattern.PatternId.Should().Be("pattern123");
        pattern.PatternType.Should().Be("Frequency Spike");
        pattern.Severity.Should().Be("High");
        pattern.Occurrences.Should().Be(15);
    }

    [Theory]
    [InlineData("Critical", "bg-danger")]
    [InlineData("High", "bg-warning text-dark")]
    [InlineData("Medium", "bg-info")]
    [InlineData("Low", "bg-success")]
    public void ClaimPattern_SeverityBadgeClass_ReturnsCorrectClass(string severity, string expectedClass)
    {
        // Arrange & Act
        var pattern = new ClaimPattern { Severity = severity };

        // Assert
        pattern.SeverityBadgeClass.Should().Be(expectedClass);
    }

    [Theory]
    [InlineData("Frequency Spike", "bi-graph-up-arrow")]
    [InlineData("Timing Anomaly", "bi-clock-history")]
    [InlineData("Amount Anomaly", "bi-currency-dollar")]
    [InlineData("Duplicate Pattern", "bi-files")]
    [InlineData("Geographic Anomaly", "bi-geo-alt")]
    [InlineData("Behavioral", "bi-person-exclamation")]
    public void ClaimPattern_IconClass_ReturnsCorrectIcon(string patternType, string expectedIcon)
    {
        // Arrange & Act
        var pattern = new ClaimPattern { PatternType = patternType };

        // Assert
        pattern.IconClass.Should().Be(expectedIcon);
    }
}
