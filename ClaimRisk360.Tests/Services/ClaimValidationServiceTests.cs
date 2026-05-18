using ClaimRisk360.Data;
using ClaimRisk360.Models;
using ClaimRisk360.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ClaimRisk360.Tests.Services;

public class ClaimValidationServiceTests
{
    private readonly Mock<ReferenceDataRepository> _mockRefData;
    private readonly ClaimValidationService _service;

    public ClaimValidationServiceTests()
    {
        _mockRefData = new Mock<ReferenceDataRepository>();
        var mockApiClient = new Mock<ClaimRisk360ApiClient>(
            new HttpClient(), Mock.Of<ILogger<ClaimRisk360ApiClient>>());
        var mockLogger = Mock.Of<ILogger<ClaimValidationService>>();
        _service = new ClaimValidationService(_mockRefData.Object, mockApiClient.Object, mockLogger);
    }

    [Fact]
    public void Validate_ValidRequest_ReturnsValid()
    {
        // Arrange
        var request = new ClaimUploadRequest
        {
            PatientName = "John Doe",
            PatientId = "P123",
            ProviderName = "Dr. Smith",
            ProviderId = "PR456",
            DiagnosisCode = "I10",
            ProcedureCode = "93000",
            Amount = 5000,
            ServiceDate = DateTime.Today.AddDays(-5),
            Location = "New York"
        };

        _mockRefData.Setup(r => r.ValidDiagnosisCodes).Returns(new HashSet<string> { "I10", "I11" });
        _mockRefData.Setup(r => r.ValidProcedureCodes).Returns(new HashSet<string> { "93000", "93005" });
        _mockRefData.Setup(r => r.ActiveProviders).Returns(new HashSet<string> { "PR456", "PR789" });

        // Act
        var result = _service.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.Status.Should().Be("Passed");
    }

    [Fact]
    public void Validate_MissingPatientName_ReturnsError()
    {
        // Arrange
        var request = new ClaimUploadRequest
        {
            PatientName = "",
            PatientId = "P123",
            ProviderName = "Dr. Smith",
            ProviderId = "PR456",
            DiagnosisCode = "I10",
            ProcedureCode = "93000",
            Amount = 5000,
            ServiceDate = DateTime.Today.AddDays(-5)
        };

        _mockRefData.Setup(r => r.ValidDiagnosisCodes).Returns(new HashSet<string> { "I10" });
        _mockRefData.Setup(r => r.ValidProcedureCodes).Returns(new HashSet<string> { "93000" });
        _mockRefData.Setup(r => r.ActiveProviders).Returns(new HashSet<string> { "PR456" });

        // Act
        var result = _service.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Field == "PatientName" && e.Code == "REQUIRED");
    }

    [Fact]
    public void Validate_MissingMultipleRequiredFields_ReturnsMultipleErrors()
    {
        // Arrange
        var request = new ClaimUploadRequest
        {
            PatientName = "",
            PatientId = "",
            ProviderName = "",
            ProviderId = "",
            DiagnosisCode = "",
            ProcedureCode = "",
            Amount = 0
        };

        _mockRefData.Setup(r => r.ValidDiagnosisCodes).Returns(new HashSet<string>());
        _mockRefData.Setup(r => r.ValidProcedureCodes).Returns(new HashSet<string>());
        _mockRefData.Setup(r => r.ActiveProviders).Returns(new HashSet<string>());

        // Act
        var result = _service.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThan(5);
    }

    [Fact]
    public void Validate_InvalidAmount_ReturnsError()
    {
        // Arrange
        var request = new ClaimUploadRequest
        {
            PatientName = "John Doe",
            PatientId = "P123",
            ProviderName = "Dr. Smith",
            ProviderId = "PR456",
            DiagnosisCode = "I10",
            ProcedureCode = "93000",
            Amount = -100,
            ServiceDate = DateTime.Today
        };

        _mockRefData.Setup(r => r.ValidDiagnosisCodes).Returns(new HashSet<string> { "I10" });
        _mockRefData.Setup(r => r.ValidProcedureCodes).Returns(new HashSet<string> { "93000" });
        _mockRefData.Setup(r => r.ActiveProviders).Returns(new HashSet<string> { "PR456" });

        // Act
        var result = _service.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Field == "Amount" && e.Code == "INVALID_AMOUNT");
    }

    [Fact]
    public void Validate_FutureServiceDate_ReturnsError()
    {
        // Arrange
        var request = new ClaimUploadRequest
        {
            PatientName = "John Doe",
            PatientId = "P123",
            ProviderName = "Dr. Smith",
            ProviderId = "PR456",
            DiagnosisCode = "I10",
            ProcedureCode = "93000",
            Amount = 5000,
            ServiceDate = DateTime.Today.AddDays(5)
        };

        _mockRefData.Setup(r => r.ValidDiagnosisCodes).Returns(new HashSet<string> { "I10" });
        _mockRefData.Setup(r => r.ValidProcedureCodes).Returns(new HashSet<string> { "93000" });
        _mockRefData.Setup(r => r.ActiveProviders).Returns(new HashSet<string> { "PR456" });

        // Act
        var result = _service.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "FUTURE_DATE");
    }

    [Fact]
    public void Validate_StaleClaimDate_ReturnsError()
    {
        // Arrange
        var request = new ClaimUploadRequest
        {
            PatientName = "John Doe",
            PatientId = "P123",
            ProviderName = "Dr. Smith",
            ProviderId = "PR456",
            DiagnosisCode = "I10",
            ProcedureCode = "93000",
            Amount = 5000,
            ServiceDate = DateTime.Today.AddYears(-2)
        };

        _mockRefData.Setup(r => r.ValidDiagnosisCodes).Returns(new HashSet<string> { "I10" });
        _mockRefData.Setup(r => r.ValidProcedureCodes).Returns(new HashSet<string> { "93000" });
        _mockRefData.Setup(r => r.ActiveProviders).Returns(new HashSet<string> { "PR456" });

        // Act
        var result = _service.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "STALE_CLAIM");
    }

    [Fact]
    public void Validate_InvalidDiagnosisCode_ReturnsError()
    {
        // Arrange
        var request = new ClaimUploadRequest
        {
            PatientName = "John Doe",
            PatientId = "P123",
            ProviderName = "Dr. Smith",
            ProviderId = "PR456",
            DiagnosisCode = "INVALID",
            ProcedureCode = "93000",
            Amount = 5000,
            ServiceDate = DateTime.Today.AddDays(-5)
        };

        _mockRefData.Setup(r => r.ValidDiagnosisCodes).Returns(new HashSet<string> { "I10", "I11" });
        _mockRefData.Setup(r => r.ValidProcedureCodes).Returns(new HashSet<string> { "93000" });
        _mockRefData.Setup(r => r.ActiveProviders).Returns(new HashSet<string> { "PR456" });

        // Act
        var result = _service.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Field == "DiagnosisCode" && e.Code == "INVALID_CODE");
    }

    [Fact]
    public void Validate_InvalidProcedureCode_ReturnsError()
    {
        // Arrange
        var request = new ClaimUploadRequest
        {
            PatientName = "John Doe",
            PatientId = "P123",
            ProviderName = "Dr. Smith",
            ProviderId = "PR456",
            DiagnosisCode = "I10",
            ProcedureCode = "INVALID",
            Amount = 5000,
            ServiceDate = DateTime.Today.AddDays(-5)
        };

        _mockRefData.Setup(r => r.ValidDiagnosisCodes).Returns(new HashSet<string> { "I10" });
        _mockRefData.Setup(r => r.ValidProcedureCodes).Returns(new HashSet<string> { "93000", "93005" });
        _mockRefData.Setup(r => r.ActiveProviders).Returns(new HashSet<string> { "PR456" });

        // Act
        var result = _service.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Field == "ProcedureCode" && e.Code == "INVALID_CODE");
    }

    [Fact]
    public void Validate_InactiveProvider_ReturnsError()
    {
        // Arrange
        var request = new ClaimUploadRequest
        {
            PatientName = "John Doe",
            PatientId = "P123",
            ProviderName = "Dr. Smith",
            ProviderId = "INACTIVE",
            DiagnosisCode = "I10",
            ProcedureCode = "93000",
            Amount = 5000,
            ServiceDate = DateTime.Today.AddDays(-5)
        };

        _mockRefData.Setup(r => r.ValidDiagnosisCodes).Returns(new HashSet<string> { "I10" });
        _mockRefData.Setup(r => r.ValidProcedureCodes).Returns(new HashSet<string> { "93000" });
        _mockRefData.Setup(r => r.ActiveProviders).Returns(new HashSet<string> { "PR456" });

        // Act
        var result = _service.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "INACTIVE_PROVIDER");
    }

    [Fact]
    public void Validate_HighAmountClaim_GeneratesWarning()
    {
        // Arrange
        var request = new ClaimUploadRequest
        {
            PatientName = "John Doe",
            PatientId = "P123",
            ProviderName = "Dr. Smith",
            ProviderId = "PR456",
            DiagnosisCode = "I10",
            ProcedureCode = "93000",
            Amount = 75000,
            ServiceDate = DateTime.Today.AddDays(-5),
            Location = "New York"
        };

        _mockRefData.Setup(r => r.ValidDiagnosisCodes).Returns(new HashSet<string> { "I10" });
        _mockRefData.Setup(r => r.ValidProcedureCodes).Returns(new HashSet<string> { "93000" });
        _mockRefData.Setup(r => r.ActiveProviders).Returns(new HashSet<string> { "PR456" });

        // Act
        var result = _service.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Warnings.Should().Contain(w => w.Contains("$50,000"));
    }

    [Fact]
    public void Validate_MissingLocation_GeneratesWarning()
    {
        // Arrange
        var request = new ClaimUploadRequest
        {
            PatientName = "John Doe",
            PatientId = "P123",
            ProviderName = "Dr. Smith",
            ProviderId = "PR456",
            DiagnosisCode = "I10",
            ProcedureCode = "93000",
            Amount = 5000,
            ServiceDate = DateTime.Today.AddDays(-5),
            Location = ""
        };

        _mockRefData.Setup(r => r.ValidDiagnosisCodes).Returns(new HashSet<string> { "I10" });
        _mockRefData.Setup(r => r.ValidProcedureCodes).Returns(new HashSet<string> { "93000" });
        _mockRefData.Setup(r => r.ActiveProviders).Returns(new HashSet<string> { "PR456" });

        // Act
        var result = _service.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Warnings.Should().Contain(w => w.Contains("Location"));
    }

    [Fact]
    public void Validate_BoundaryAmountValue_IsValid()
    {
        // Arrange
        var request = new ClaimUploadRequest
        {
            PatientName = "John Doe",
            PatientId = "P123",
            ProviderName = "Dr. Smith",
            ProviderId = "PR456",
            DiagnosisCode = "I10",
            ProcedureCode = "93000",
            Amount = 0.01m,
            ServiceDate = DateTime.Today.AddDays(-5),
            Location = "New York"
        };

        _mockRefData.Setup(r => r.ValidDiagnosisCodes).Returns(new HashSet<string> { "I10" });
        _mockRefData.Setup(r => r.ValidProcedureCodes).Returns(new HashSet<string> { "93000" });
        _mockRefData.Setup(r => r.ActiveProviders).Returns(new HashSet<string> { "PR456" });

        // Act
        var result = _service.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ServiceDateToday_IsValid()
    {
        // Arrange
        var request = new ClaimUploadRequest
        {
            PatientName = "John Doe",
            PatientId = "P123",
            ProviderName = "Dr. Smith",
            ProviderId = "PR456",
            DiagnosisCode = "I10",
            ProcedureCode = "93000",
            Amount = 5000,
            ServiceDate = DateTime.Today,
            Location = "New York"
        };

        _mockRefData.Setup(r => r.ValidDiagnosisCodes).Returns(new HashSet<string> { "I10" });
        _mockRefData.Setup(r => r.ValidProcedureCodes).Returns(new HashSet<string> { "93000" });
        _mockRefData.Setup(r => r.ActiveProviders).Returns(new HashSet<string> { "PR456" });

        // Act
        var result = _service.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
