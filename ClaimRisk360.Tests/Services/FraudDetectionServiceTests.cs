using ClaimRisk360.Data;
using ClaimRisk360.Models;
using ClaimRisk360.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace ClaimRisk360.Tests.Services;

public class FraudDetectionServiceTests
{
    private readonly Mock<ClaimRepository> _mockClaimRepo;
    private readonly FraudDetectionService _service;

    public FraudDetectionServiceTests()
    {
        _mockClaimRepo = new Mock<ClaimRepository>();
        _service = new FraudDetectionService(_mockClaimRepo.Object);
    }

    [Fact]
    public void GetAllClaims_ReturnsAllClaims()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new() { ClaimId = "claim1", PatientName = "John", FraudRiskScore = 20 },
            new() { ClaimId = "claim2", PatientName = "Jane", FraudRiskScore = 75 }
        };

        _mockClaimRepo.Setup(r => r.GetAllClaims()).Returns(claims);

        // Act
        var result = _service.GetAllClaims();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(c => c.ClaimId == "claim1");
        result.Should().Contain(c => c.ClaimId == "claim2");
    }

    [Fact]
    public void GetAllClaims_EmptyResult_ReturnsEmptyList()
    {
        // Arrange
        _mockClaimRepo.Setup(r => r.GetAllClaims()).Returns(new List<Claim>());

        // Act
        var result = _service.GetAllClaims();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetClaim_ExistingClaim_ReturnsClaim()
    {
        // Arrange
        var claim = new Claim { ClaimId = "claim1", PatientName = "John" };
        _mockClaimRepo.Setup(r => r.GetClaim("claim1")).Returns(claim);

        // Act
        var result = _service.GetClaim("claim1");

        // Assert
        result.Should().NotBeNull();
        result?.ClaimId.Should().Be("claim1");
        result?.PatientName.Should().Be("John");
    }

    [Fact]
    public void GetClaim_NonExistentClaim_ReturnsNull()
    {
        // Arrange
        _mockClaimRepo.Setup(r => r.GetClaim("nonexistent")).Returns((Claim?)null);

        // Act
        var result = _service.GetClaim("nonexistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetDashboardStats_CalculatesCorrectly()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new() { ClaimId = "c1", FraudRiskScore = 15, Amount = 5000, FraudType = "Legitimate" },
            new() { ClaimId = "c2", FraudRiskScore = 45, Amount = 5000, FraudType = "Legitimate" },
            new() { ClaimId = "c3", FraudRiskScore = 75, Amount = 10000, FraudType = "Provider" },
            new() { ClaimId = "c4", FraudRiskScore = 85, Amount = 8000, FraudType = "Provider" }
        };

        _mockClaimRepo.Setup(r => r.GetAllClaims()).Returns(claims);
        _mockClaimRepo.Setup(r => r.GetAllFraudRings()).Returns(new List<FraudRing>());

        // Act
        var result = _service.GetDashboardStats();

        // Assert
        result.TotalClaims.Should().Be(4);
        result.FlaggedClaims.Should().Be(2);
        result.HighRiskClaims.Should().Be(2);
        result.TotalAmountAtRisk.Should().Be(18000m);
    }

    [Fact]
    public void GetDashboardStats_NoHighRiskClaims_CalculatesCorrectly()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new() { ClaimId = "c1", FraudRiskScore = 15, Amount = 5000 },
            new() { ClaimId = "c2", FraudRiskScore = 25, Amount = 5000 }
        };

        _mockClaimRepo.Setup(r => r.GetAllClaims()).Returns(claims);
        _mockClaimRepo.Setup(r => r.GetAllFraudRings()).Returns(new List<FraudRing>());

        // Act
        var result = _service.GetDashboardStats();

        // Assert
        result.HighRiskClaims.Should().Be(0);
        result.TotalAmountAtRisk.Should().Be(0);
    }

    [Fact]
    public void GetDashboardStats_GeneratesScoreDistribution()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new() { ClaimId = "c1", FraudRiskScore = 5 },
            new() { ClaimId = "c2", FraudRiskScore = 25 },
            new() { ClaimId = "c3", FraudRiskScore = 55 },
            new() { ClaimId = "c4", FraudRiskScore = 85 }
        };

        _mockClaimRepo.Setup(r => r.GetAllClaims()).Returns(claims);
        _mockClaimRepo.Setup(r => r.GetAllFraudRings()).Returns(new List<FraudRing>());

        // Act
        var result = _service.GetDashboardStats();

        // Assert
        result.ScoreDistribution.Should().HaveCount(10);
        result.ScoreDistribution.Sum().Should().Be(4);
    }

    [Fact]
    public void GetDashboardStats_CountsFraudTypes()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new() { ClaimId = "c1", FraudType = "Provider", FraudRiskScore = 0 },
            new() { ClaimId = "c2", FraudType = "Provider", FraudRiskScore = 0 },
            new() { ClaimId = "c3", FraudType = "Patient", FraudRiskScore = 0 },
            new() { ClaimId = "c4", FraudType = "Legitimate", FraudRiskScore = 0 }
        };

        _mockClaimRepo.Setup(r => r.GetAllClaims()).Returns(claims);
        _mockClaimRepo.Setup(r => r.GetAllFraudRings()).Returns(new List<FraudRing>());

        // Act
        var result = _service.GetDashboardStats();

        // Assert
        result.FraudTypeCounts.Should().HaveCount(5);
        result.FraudTypeCounts.Should().Contain(ftc => ftc.Type == "Provider Fraud" && ftc.Count == 2);
        result.FraudTypeCounts.Should().Contain(ftc => ftc.Type == "Patient Fraud" && ftc.Count == 1);
    }

    [Fact]
    public void GetFraudRings_ReturnsAllFraudRings()
    {
        // Arrange
        var rings = new List<FraudRing>
        {
            new() { RingId = "ring1", Label = "Ring 1" },
            new() { RingId = "ring2", Label = "Ring 2" }
        };

        _mockClaimRepo.Setup(r => r.GetAllFraudRings()).Returns(rings);

        // Act
        var result = _service.GetFraudRings();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public void GetFraudRings_NoRings_ReturnsEmpty()
    {
        // Arrange
        _mockClaimRepo.Setup(r => r.GetAllFraudRings()).Returns(new List<FraudRing>());

        // Act
        var result = _service.GetFraudRings();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetFraudRing_ExistingRing_ReturnsRing()
    {
        // Arrange
        var ring = new FraudRing { RingId = "ring1", Label = "Test Ring" };
        _mockClaimRepo.Setup(r => r.GetFraudRing("ring1")).Returns(ring);

        // Act
        var result = _service.GetFraudRing("ring1");

        // Assert
        result.Should().NotBeNull();
        result?.RingId.Should().Be("ring1");
    }

    [Fact]
    public void GetFraudRing_NonExistentRing_ReturnsNull()
    {
        // Arrange
        _mockClaimRepo.Setup(r => r.GetFraudRing("nonexistent")).Returns((FraudRing?)null);

        // Act
        var result = _service.GetFraudRing("nonexistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetExplainability_ExistingClaim_LowRisk_ReturnsAutoApprove()
    {
        // Arrange
        var claim = new Claim 
        { 
            ClaimId = "claim1", 
            FraudRiskScore = 20,
            RiskReasons = new List<string>()
        };
        _mockClaimRepo.Setup(r => r.GetClaim("claim1")).Returns(claim);

        // Act
        var result = _service.GetExplainability("claim1");

        // Assert
        result.ClaimId.Should().Be("claim1");
        result.FraudRiskScore.Should().Be(20);
        result.Decision.Should().Be("Auto-Approve");
        result.ModelUsed.Should().Contain("Isolation Forest");
    }

    [Fact]
    public void GetExplainability_ExistingClaim_MediumRisk_ReturnsMonitor()
    {
        // Arrange
        var claim = new Claim 
        { 
            ClaimId = "claim2", 
            FraudRiskScore = 50,
            RiskReasons = new List<string>()
        };
        _mockClaimRepo.Setup(r => r.GetClaim("claim2")).Returns(claim);

        // Act
        var result = _service.GetExplainability("claim2");

        // Assert
        result.ClaimId.Should().Be("claim2");
        result.Decision.Should().Be("Monitor");
    }

    [Fact]
    public void GetExplainability_ExistingClaim_HighRisk_ReturnsFlagForAudit()
    {
        // Arrange
        var claim = new Claim 
        { 
            ClaimId = "claim3", 
            FraudRiskScore = 80,
            RiskReasons = new List<string>()
        };
        _mockClaimRepo.Setup(r => r.GetClaim("claim3")).Returns(claim);

        // Act
        var result = _service.GetExplainability("claim3");

        // Assert
        result.ClaimId.Should().Be("claim3");
        result.Decision.Should().Be("Flag for Manual Audit");
    }

    [Fact]
    public void GetExplainability_NonExistentClaim_ReturnsEmpty()
    {
        // Arrange
        _mockClaimRepo.Setup(r => r.GetClaim("nonexistent")).Returns((Claim?)null);

        // Act
        var result = _service.GetExplainability("nonexistent");

        // Assert
        result.ClaimId.Should().Be(string.Empty);
    }

    [Fact]
    public void GetExplainability_GeneratesFeatureContributions()
    {
        // Arrange
        var claim = new Claim 
        { 
            ClaimId = "claim1", 
            FraudRiskScore = 75,
            Amount = 10000,
            FraudType = "Collusion",
            RiskReasons = new List<string>()
        };
        _mockClaimRepo.Setup(r => r.GetClaim("claim1")).Returns(claim);

        // Act
        var result = _service.GetExplainability("claim1");

        // Assert
        result.Features.Should().NotBeEmpty();
        result.Features.Should().Contain(f => f.FeatureName == "Billing Frequency");
        result.Features.Should().Contain(f => f.FeatureName == "Amount vs Peer Average");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(30)]
    [InlineData(50)]
    [InlineData(100)]
    public void GetExplainability_MultipleScores_CalculateCorrectDecision(int score)
    {
        // Arrange
        var claim = new Claim 
        { 
            ClaimId = $"claim_{score}", 
            FraudRiskScore = score,
            RiskReasons = new List<string>()
        };
        _mockClaimRepo.Setup(r => r.GetClaim($"claim_{score}")).Returns(claim);

        // Act
        var result = _service.GetExplainability($"claim_{score}");

        // Assert
        result.Decision.Should().NotBeNullOrEmpty();
        result.Decision.Should().BeOneOf("Auto-Approve", "Monitor", "Flag for Manual Audit");
    }
}
