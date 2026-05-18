using ClaimRisk360.Models;
using FluentAssertions;
using Xunit;

namespace ClaimRisk360.Tests.Models;

public class ClaimUploadRequestTests
{
    [Fact]
    public void ClaimUploadRequest_DefaultConstructor_InitializesWithDefaults()
    {
        // Arrange & Act
        var request = new ClaimUploadRequest();

        // Assert
        request.PatientName.Should().Be(string.Empty);
        request.PatientId.Should().Be(string.Empty);
        request.ProviderName.Should().Be(string.Empty);
        request.ProviderId.Should().Be(string.Empty);
        request.Specialty.Should().Be(string.Empty);
        request.DiagnosisCode.Should().Be(string.Empty);
        request.ProcedureCode.Should().Be(string.Empty);
        request.Amount.Should().Be(0);
        request.ServiceDate.Should().Be(DateTime.Today);
        request.Location.Should().Be(string.Empty);
    }

    [Fact]
    public void ClaimUploadRequest_SetAllProperties_UpdatesCorrectly()
    {
        // Arrange
        var request = new ClaimUploadRequest();
        var serviceDate = DateTime.Today.AddDays(-5);
        var amount = 5000m;

        // Act
        request.PatientName = "John Smith";
        request.PatientId = "P123";
        request.ProviderName = "Dr. Smith Clinic";
        request.ProviderId = "PR456";
        request.Specialty = "Cardiology";
        request.DiagnosisCode = "I10";
        request.ProcedureCode = "93000";
        request.Amount = amount;
        request.ServiceDate = serviceDate;
        request.Location = "New York";

        // Assert
        request.PatientName.Should().Be("John Smith");
        request.PatientId.Should().Be("P123");
        request.ProviderName.Should().Be("Dr. Smith Clinic");
        request.ProviderId.Should().Be("PR456");
        request.Specialty.Should().Be("Cardiology");
        request.DiagnosisCode.Should().Be("I10");
        request.ProcedureCode.Should().Be("93000");
        request.Amount.Should().Be(amount);
        request.ServiceDate.Should().Be(serviceDate);
        request.Location.Should().Be("New York");
    }

    [Theory]
    [InlineData(100)]
    [InlineData(5000)]
    [InlineData(50000)]
    [InlineData(100000)]
    public void ClaimUploadRequest_DifferentAmounts_AllValid(decimal amount)
    {
        // Arrange & Act
        var request = new ClaimUploadRequest { Amount = amount };

        // Assert
        request.Amount.Should().Be(amount);
    }

    [Fact]
    public void ClaimUploadRequest_PastServiceDates_AreValid()
    {
        // Arrange
        var request = new ClaimUploadRequest();
        var pastDate = DateTime.Today.AddMonths(-3);

        // Act
        request.ServiceDate = pastDate;

        // Assert
        request.ServiceDate.Should().Be(pastDate);
    }
}

public class ValidationResultTests
{
    [Fact]
    public void ValidationResult_NoErrors_IsValidTrue()
    {
        // Arrange & Act
        var result = new ValidationResult();

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.Status.Should().Be("Passed");
    }

    [Fact]
    public void ValidationResult_WithErrors_IsValidFalse()
    {
        // Arrange
        var result = new ValidationResult();
        result.Errors.Add(new() { Field = "Amount", Code = "INVALID", Message = "Amount is invalid" });

        // Act & Assert
        result.IsValid.Should().BeFalse();
        result.Status.Should().Be("Rejected");
    }

    [Fact]
    public void ValidationResult_MultipleErrors_AllRecorded()
    {
        // Arrange
        var result = new ValidationResult();

        // Act
        result.Errors.Add(new() { Field = "PatientName", Code = "REQUIRED", Message = "Patient name is required" });
        result.Errors.Add(new() { Field = "Amount", Code = "INVALID", Message = "Amount must be positive" });
        result.Errors.Add(new() { Field = "ProviderId", Code = "INVALID", Message = "Provider not found" });

        // Assert
        result.Errors.Should().HaveCount(3);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ValidationResult_WithWarnings_StillValid()
    {
        // Arrange
        var result = new ValidationResult();

        // Act
        result.Warnings.Add("Claim amount exceeds threshold");
        result.Warnings.Add("Missing location data");

        // Assert
        result.IsValid.Should().BeTrue();
        result.Warnings.Should().HaveCount(2);
    }
}

public class ValidationErrorTests
{
    [Fact]
    public void ValidationError_DefaultConstructor_InitializesEmpty()
    {
        // Arrange & Act
        var error = new ValidationError();

        // Assert
        error.Field.Should().Be(string.Empty);
        error.Code.Should().Be(string.Empty);
        error.Message.Should().Be(string.Empty);
    }

    [Fact]
    public void ValidationError_SetProperties_UpdatesCorrectly()
    {
        // Arrange
        var error = new ValidationError();

        // Act
        error.Field = "Amount";
        error.Code = "INVALID_AMOUNT";
        error.Message = "Amount must be greater than zero";

        // Assert
        error.Field.Should().Be("Amount");
        error.Code.Should().Be("INVALID_AMOUNT");
        error.Message.Should().Be("Amount must be greater than zero");
    }
}

public class AuditEntryTests
{
    [Fact]
    public void AuditEntry_DefaultConstructor_InitializesWithDefaults()
    {
        // Arrange & Act
        var entry = new AuditEntry();

        // Assert
        entry.AuditId.Should().Be(string.Empty);
        entry.ClaimId.Should().Be(string.Empty);
        entry.Action.Should().Be(string.Empty);
        entry.PerformedBy.Should().Be(string.Empty);
        entry.Details.Should().Be(string.Empty);
        entry.Category.Should().Be(string.Empty);
        entry.CaseReviewId.Should().BeNull();
    }

    [Fact]
    public void AuditEntry_SetAllProperties_UpdatesCorrectly()
    {
        // Arrange
        var entry = new AuditEntry();
        var timestamp = DateTime.UtcNow;

        // Act
        entry.AuditId = "audit123";
        entry.ClaimId = "claim456";
        entry.Action = "Approved";
        entry.PerformedBy = "user789";
        entry.Timestamp = timestamp;
        entry.Details = "Claim approved after review";
        entry.Category = "Approval";
        entry.CaseReviewId = "case123";

        // Assert
        entry.AuditId.Should().Be("audit123");
        entry.ClaimId.Should().Be("claim456");
        entry.Action.Should().Be("Approved");
        entry.PerformedBy.Should().Be("user789");
        entry.Timestamp.Should().Be(timestamp);
        entry.Details.Should().Be("Claim approved after review");
        entry.Category.Should().Be("Approval");
        entry.CaseReviewId.Should().Be("case123");
    }
}

public class CaseReviewTests
{
    [Fact]
    public void CaseReview_DefaultConstructor_InitializesWithDefaults()
    {
        // Arrange & Act
        var caseReview = new CaseReview();

        // Assert
        caseReview.CaseId.Should().Be(string.Empty);
        caseReview.ClaimId.Should().Be(string.Empty);
        caseReview.AssignedTo.Should().Be(string.Empty);
        caseReview.Status.Should().Be("Open");
        caseReview.Priority.Should().Be("Medium");
        caseReview.Decision.Should().Be(string.Empty);
        caseReview.Justification.Should().Be(string.Empty);
        caseReview.History.Should().BeEmpty();
        caseReview.ResolvedAt.Should().BeNull();
    }

    [Fact]
    public void CaseReview_SetAllProperties_UpdatesCorrectly()
    {
        // Arrange
        var caseReview = new CaseReview();
        var createdAt = DateTime.UtcNow;
        var resolvedAt = DateTime.UtcNow.AddDays(3);

        // Act
        caseReview.CaseId = "case123";
        caseReview.ClaimId = "claim456";
        caseReview.AssignedTo = "investigator1";
        caseReview.Status = "In Review";
        caseReview.Priority = "High";
        caseReview.CreatedAt = createdAt;
        caseReview.ResolvedAt = resolvedAt;
        caseReview.Decision = "Approved";
        caseReview.Justification = "Claim appears legitimate";

        // Assert
        caseReview.CaseId.Should().Be("case123");
        caseReview.ClaimId.Should().Be("claim456");
        caseReview.AssignedTo.Should().Be("investigator1");
        caseReview.Status.Should().Be("In Review");
        caseReview.Priority.Should().Be("High");
        caseReview.CreatedAt.Should().Be(createdAt);
        caseReview.ResolvedAt.Should().Be(resolvedAt);
        caseReview.Decision.Should().Be("Approved");
        caseReview.Justification.Should().Be("Claim appears legitimate");
    }

    [Theory]
    [InlineData("Open", "bg-warning text-dark")]
    [InlineData("In Review", "bg-info")]
    [InlineData("Escalated", "bg-danger")]
    [InlineData("Resolved", "bg-success")]
    public void CaseReview_StatusBadgeClass_ReturnsCorrectClass(string status, string expectedClass)
    {
        // Arrange & Act
        var caseReview = new CaseReview { Status = status };

        // Assert
        caseReview.StatusBadgeClass.Should().Be(expectedClass);
    }

    [Theory]
    [InlineData("Critical", "bg-danger")]
    [InlineData("High", "bg-warning text-dark")]
    [InlineData("Medium", "bg-info")]
    [InlineData("Low", "bg-success")]
    public void CaseReview_PriorityBadgeClass_ReturnsCorrectClass(string priority, string expectedClass)
    {
        // Arrange & Act
        var caseReview = new CaseReview { Priority = priority };

        // Assert
        caseReview.PriorityBadgeClass.Should().Be(expectedClass);
    }

    [Fact]
    public void CaseReview_AddHistory_UpdatesCorrectly()
    {
        // Arrange
        var caseReview = new CaseReview();
        var auditEntry = new AuditEntry { Action = "Created", PerformedBy = "user1" };

        // Act
        caseReview.History.Add(auditEntry);

        // Assert
        caseReview.History.Should().HaveCount(1);
        caseReview.History[0].Action.Should().Be("Created");
    }
}
