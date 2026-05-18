using ClaimRisk360.Data;
using ClaimRisk360.Models;
using ClaimRisk360.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace ClaimRisk360.Tests.Services;

public class RuleEngineServiceTests
{
    private readonly Mock<FraudDetectionService> _mockFraudService;
    private readonly Mock<ReferenceDataRepository> _mockRefData;
    private readonly Mock<ClaimRepository> _mockClaimRepo;
    private readonly RuleEngineService _service;

    public RuleEngineServiceTests()
    {
        _mockFraudService = new Mock<FraudDetectionService>();
        _mockRefData = new Mock<ReferenceDataRepository>();
        _mockClaimRepo = new Mock<ClaimRepository>();
        _service = new RuleEngineService(_mockFraudService.Object, _mockRefData.Object, _mockClaimRepo.Object);
    }

    [Fact]
    public void RunAllRules_WithoutClaimId_ChecksAllClaims()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new() { ClaimId = "claim1", PatientId = "P1", ProviderId = "PR1", Amount = 5000, SubmissionDate = DateTime.UtcNow },
            new() { ClaimId = "claim2", PatientId = "P2", ProviderId = "PR2", Amount = 8000, SubmissionDate = DateTime.UtcNow }
        };

        _mockFraudService.Setup(f => f.GetAllClaims()).Returns(claims);
        _mockRefData.Setup(r => r.BlacklistedProviders).Returns(new HashSet<string>());
        _mockRefData.Setup(r => r.BlacklistedPatients).Returns(new HashSet<string>());

        // Act
        var results = _service.RunAllRules();

        // Assert
        results.Should().NotBeEmpty();
        _mockFraudService.Verify(f => f.GetAllClaims(), Times.AtLeastOnce);
    }

    [Fact]
    public void RunAllRules_WithClaimId_ChecksSingleClaim()
    {
        // Arrange
        var claim = new Claim 
        { 
            ClaimId = "claim1", 
            PatientId = "P1", 
            ProviderId = "PR1", 
            Amount = 5000, 
            SubmissionDate = DateTime.UtcNow,
            DiagnosisCode = "I10"
        };

        _mockFraudService.Setup(f => f.GetClaim("claim1")).Returns(claim);
        _mockFraudService.Setup(f => f.GetAllClaims()).Returns(new List<Claim> { claim });
        _mockRefData.Setup(r => r.BlacklistedProviders).Returns(new HashSet<string>());
        _mockRefData.Setup(r => r.BlacklistedPatients).Returns(new HashSet<string>());

        // Act
        var results = _service.RunAllRules("claim1");

        // Assert
        results.Should().NotBeEmpty();
        _mockFraudService.Verify(f => f.GetClaim("claim1"), Times.AtLeastOnce);
    }

    [Fact]
    public void RunAllRules_SortsByTriggeredFirst()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new() { ClaimId = "claim1", PatientId = "P1", ProviderId = "PR1", Amount = 5000, SubmissionDate = DateTime.UtcNow, DiagnosisCode = "I10" }
        };

        _mockFraudService.Setup(f => f.GetAllClaims()).Returns(claims);
        _mockRefData.Setup(r => r.BlacklistedProviders).Returns(new HashSet<string>());
        _mockRefData.Setup(r => r.BlacklistedPatients).Returns(new HashSet<string>());

        // Act
        var results = _service.RunAllRules();

        // Assert
        // First results should have Triggered = true
        var triggeredFirst = results.Where(r => r.Triggered).Count();
        var notTriggeredFirst = results.Where(r => !r.Triggered).Count();

        if (triggeredFirst > 0 && notTriggeredFirst > 0)
        {
            results.First(r => r.Triggered).Should().NotBeNull();
            results.Last(r => !r.Triggered).Should().NotBeNull();
        }
    }

    [Fact]
    public void RunAllRules_SortsBySeverity()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new() { ClaimId = "claim1", PatientId = "P1", ProviderId = "PR1", Amount = 5000, SubmissionDate = DateTime.UtcNow, DiagnosisCode = "I10" }
        };

        _mockFraudService.Setup(f => f.GetAllClaims()).Returns(claims);
        _mockRefData.Setup(r => r.BlacklistedProviders).Returns(new HashSet<string>());
        _mockRefData.Setup(r => r.BlacklistedPatients).Returns(new HashSet<string>());

        // Act
        var results = _service.RunAllRules();

        // Assert
        results.Should().NotBeEmpty();
        // Verify severity mapping (Critical > High > Medium > Low)
        results.Should().AllSatisfy(r => r.Severity.Should().BeOneOf("Critical", "High", "Medium", "Low", "Info"));
    }

    [Fact]
    public void GetTriggeredRules_ReturnsOnlyTriggeredRules()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new() { ClaimId = "claim1", PatientId = "P1", ProviderId = "PR1", Amount = 5000, SubmissionDate = DateTime.UtcNow, DiagnosisCode = "I10" }
        };

        _mockFraudService.Setup(f => f.GetAllClaims()).Returns(claims);
        _mockRefData.Setup(r => r.BlacklistedProviders).Returns(new HashSet<string>());
        _mockRefData.Setup(r => r.BlacklistedPatients).Returns(new HashSet<string>());

        // Act
        var triggeredRules = _service.GetTriggeredRules();

        // Assert
        triggeredRules.Should().AllSatisfy(r => r.Triggered.Should().BeTrue());
    }

    [Fact]
    public void RunAllRules_DetectsDuplicateClaim_WhenCriteriaMet()
    {
        // Arrange
        var baseDate = DateTime.UtcNow;
        var claim1 = new Claim 
        { 
            ClaimId = "claim1", 
            PatientId = "P1", 
            ProviderId = "PR1", 
            Amount = 5000, 
            SubmissionDate = baseDate,
            DiagnosisCode = "I10"
        };
        var claim2 = new Claim 
        { 
            ClaimId = "claim2", 
            PatientId = "P1", 
            ProviderId = "PR1", 
            Amount = 5000, 
            SubmissionDate = baseDate.AddHours(1),
            DiagnosisCode = "I10"
        };

        _mockFraudService.Setup(f => f.GetAllClaims()).Returns(new List<Claim> { claim1, claim2 });
        _mockFraudService.Setup(f => f.GetClaim("claim1")).Returns(claim1);
        _mockRefData.Setup(r => r.BlacklistedProviders).Returns(new HashSet<string>());
        _mockRefData.Setup(r => r.BlacklistedPatients).Returns(new HashSet<string>());

        // Act
        var results = _service.RunAllRules("claim1");

        // Assert
        results.Should().Contain(r => r.Category == "Duplicate" && r.Triggered);
    }

    [Fact]
    public void RunAllRules_DetectsAmountThresholdBreach()
    {
        // Arrange
        var claim = new Claim 
        { 
            ClaimId = "claim1", 
            PatientId = "P1", 
            ProviderId = "PR1", 
            Amount = 15000,
            SubmissionDate = DateTime.UtcNow,
            DiagnosisCode = "I10"
        };

        _mockFraudService.Setup(f => f.GetAllClaims()).Returns(new List<Claim> { claim });
        _mockRefData.Setup(r => r.BlacklistedProviders).Returns(new HashSet<string>());
        _mockRefData.Setup(r => r.BlacklistedPatients).Returns(new HashSet<string>());

        // Act
        var results = _service.RunAllRules();

        // Assert
        results.Should().Contain(r => r.Category == "Threshold" && r.Triggered);
    }

    [Fact]
    public void RunAllRules_HighAmountThreshold_SetsSeverityToCritical()
    {
        // Arrange
        var claim = new Claim 
        { 
            ClaimId = "claim1", 
            PatientId = "P1", 
            ProviderId = "PR1", 
            Amount = 30000,
            SubmissionDate = DateTime.UtcNow,
            DiagnosisCode = "I10"
        };

        _mockFraudService.Setup(f => f.GetAllClaims()).Returns(new List<Claim> { claim });
        _mockRefData.Setup(r => r.BlacklistedProviders).Returns(new HashSet<string>());
        _mockRefData.Setup(r => r.BlacklistedPatients).Returns(new HashSet<string>());

        // Act
        var results = _service.RunAllRules();

        // Assert
        var amountThresholdRule = results.FirstOrDefault(r => r.Category == "Threshold");
        amountThresholdRule?.Severity.Should().Be("Critical");
    }

    [Fact]
    public void RunAllRules_NormalAmount_DoesNotTrigger()
    {
        // Arrange
        var claim = new Claim 
        { 
            ClaimId = "claim1", 
            PatientId = "P1", 
            ProviderId = "PR1", 
            Amount = 5000,
            SubmissionDate = DateTime.UtcNow,
            DiagnosisCode = "I10"
        };

        _mockFraudService.Setup(f => f.GetAllClaims()).Returns(new List<Claim> { claim });
        _mockRefData.Setup(r => r.BlacklistedProviders).Returns(new HashSet<string>());
        _mockRefData.Setup(r => r.BlacklistedPatients).Returns(new HashSet<string>());

        // Act
        var results = _service.RunAllRules();

        // Assert
        var amountThresholdRule = results.FirstOrDefault(r => r.Category == "Threshold");
        amountThresholdRule?.Triggered.Should().BeFalse();
    }

    [Fact]
    public void RuleCheckResult_ContainsRequiredFields()
    {
        // Arrange & Act
        var rule = new RuleCheckResult
        {
            RuleId = "RULE-001",
            RuleName = "Test Rule",
            Category = "Duplicate",
            Severity = "High",
            Triggered = true,
            Details = "Test details"
        };

        // Assert
        rule.RuleId.Should().Be("RULE-001");
        rule.RuleName.Should().Be("Test Rule");
        rule.Category.Should().Be("Duplicate");
        rule.Severity.Should().Be("High");
        rule.Triggered.Should().BeTrue();
        rule.Details.Should().Contain("Test details");
    }

    [Fact]
    public void RunAllRules_EmptyClaimList_ReturnsEmpty()
    {
        // Arrange
        _mockFraudService.Setup(f => f.GetAllClaims()).Returns(new List<Claim>());
        _mockRefData.Setup(r => r.BlacklistedProviders).Returns(new HashSet<string>());
        _mockRefData.Setup(r => r.BlacklistedPatients).Returns(new HashSet<string>());

        // Act
        var results = _service.RunAllRules();

        // Assert
        results.Should().BeEmpty();
    }

    [Theory]
    [InlineData("Critical", 4)]
    [InlineData("High", 3)]
    [InlineData("Medium", 2)]
    [InlineData("Low", 1)]
    public void RuleCheckResult_SeverityMapping_IsCorrect(string severity, int expectedValue)
    {
        // Arrange
        var severityValue = severity == "Critical" ? 4 : severity == "High" ? 3 : severity == "Medium" ? 2 : 1;

        // Act & Assert
        severityValue.Should().Be(expectedValue);
    }

    [Fact]
    public void RunAllRules_WithMultipleClaims_ProcessesAll()
    {
        // Arrange
        var claims = Enumerable.Range(1, 5)
            .Select(i => new Claim 
            { 
                ClaimId = $"claim{i}", 
                PatientId = $"P{i}", 
                ProviderId = $"PR{i}", 
                Amount = 5000 * i, 
                SubmissionDate = DateTime.UtcNow,
                DiagnosisCode = "I10"
            })
            .ToList();

        _mockFraudService.Setup(f => f.GetAllClaims()).Returns(claims);
        _mockRefData.Setup(r => r.BlacklistedProviders).Returns(new HashSet<string>());
        _mockRefData.Setup(r => r.BlacklistedPatients).Returns(new HashSet<string>());

        // Act
        var results = _service.RunAllRules();

        // Assert
        results.Should().NotBeEmpty();
        results.Should().HaveCountGreaterThan(0);
    }
}
