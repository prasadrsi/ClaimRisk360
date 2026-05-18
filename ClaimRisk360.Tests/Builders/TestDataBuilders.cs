using ClaimRisk360.Models;

namespace ClaimRisk360.Tests.Builders;

/// <summary>
/// Test data builder for creating Claim objects with fluent syntax
/// </summary>
public class ClaimBuilder
{
    private string _claimId = "claim-default";
    private string _patientName = "Test Patient";
    private string _patientId = "P-default";
    private string _providerName = "Test Provider";
    private string _providerId = "PR-default";
    private string _specialty = "General";
    private string _diagnosisCode = "I10";
    private string _procedureCode = "99213";
    private decimal _amount = 5000m;
    private DateTime _submissionDate = DateTime.UtcNow;
    private string _location = "Test Location";
    private int _fraudRiskScore = 0;
    private string _fraudType = "Legitimate";
    private string _status = "Pending";
    private string _approvalStatus = "Pending";

    public ClaimBuilder WithClaimId(string claimId)
    {
        _claimId = claimId;
        return this;
    }

    public ClaimBuilder WithPatient(string name, string id)
    {
        _patientName = name;
        _patientId = id;
        return this;
    }

    public ClaimBuilder WithProvider(string name, string id)
    {
        _providerName = name;
        _providerId = id;
        return this;
    }

    public ClaimBuilder WithAmount(decimal amount)
    {
        _amount = amount;
        return this;
    }

    public ClaimBuilder WithFraudRiskScore(int score)
    {
        _fraudRiskScore = score;
        return this;
    }

    public ClaimBuilder WithDiagnosisCode(string code)
    {
        _diagnosisCode = code;
        return this;
    }

    public ClaimBuilder WithProcedureCode(string code)
    {
        _procedureCode = code;
        return this;
    }

    public ClaimBuilder WithSubmissionDate(DateTime date)
    {
        _submissionDate = date;
        return this;
    }

    public ClaimBuilder WithStatus(string status)
    {
        _status = status;
        return this;
    }

    public ClaimBuilder WithApprovalStatus(string status)
    {
        _approvalStatus = status;
        return this;
    }

    public Claim Build()
    {
        return new Claim
        {
            ClaimId = _claimId,
            PatientName = _patientName,
            PatientId = _patientId,
            ProviderName = _providerName,
            ProviderId = _providerId,
            Specialty = _specialty,
            DiagnosisCode = _diagnosisCode,
            ProcedureCode = _procedureCode,
            Amount = _amount,
            SubmissionDate = _submissionDate,
            Location = _location,
            FraudRiskScore = _fraudRiskScore,
            FraudType = _fraudType,
            Status = _status,
            ApprovalStatus = _approvalStatus
        };
    }
}

/// <summary>
/// Test data builder for creating ClaimUploadRequest objects
/// </summary>
public class ClaimUploadRequestBuilder
{
    private string _patientName = "Test Patient";
    private string _patientId = "P-default";
    private string _providerName = "Test Provider";
    private string _providerId = "PR-default";
    private string _specialty = "General";
    private string _diagnosisCode = "I10";
    private string _procedureCode = "99213";
    private decimal _amount = 5000m;
    private DateTime _serviceDate = DateTime.Today;
    private string _location = "Test Location";

    public ClaimUploadRequestBuilder WithPatient(string name, string id)
    {
        _patientName = name;
        _patientId = id;
        return this;
    }

    public ClaimUploadRequestBuilder WithProvider(string name, string id)
    {
        _providerName = name;
        _providerId = id;
        return this;
    }

    public ClaimUploadRequestBuilder WithAmount(decimal amount)
    {
        _amount = amount;
        return this;
    }

    public ClaimUploadRequestBuilder WithServiceDate(DateTime date)
    {
        _serviceDate = date;
        return this;
    }

    public ClaimUploadRequestBuilder WithCodes(string diagnosisCode, string procedureCode)
    {
        _diagnosisCode = diagnosisCode;
        _procedureCode = procedureCode;
        return this;
    }

    public ClaimUploadRequest Build()
    {
        return new ClaimUploadRequest
        {
            PatientName = _patientName,
            PatientId = _patientId,
            ProviderName = _providerName,
            ProviderId = _providerId,
            Specialty = _specialty,
            DiagnosisCode = _diagnosisCode,
            ProcedureCode = _procedureCode,
            Amount = _amount,
            ServiceDate = _serviceDate,
            Location = _location
        };
    }
}

/// <summary>
/// Test data builder for creating CaseReview objects
/// </summary>
public class CaseReviewBuilder
{
    private string _caseId = "case-default";
    private string _claimId = "claim-default";
    private string _assignedTo = "investigator1";
    private string _status = "Open";
    private string _priority = "Medium";
    private DateTime _createdAt = DateTime.UtcNow;
    private DateTime? _resolvedAt;
    private string _decision = "";
    private string _justification = "";

    public CaseReviewBuilder WithCaseId(string caseId)
    {
        _caseId = caseId;
        return this;
    }

    public CaseReviewBuilder WithClaimId(string claimId)
    {
        _claimId = claimId;
        return this;
    }

    public CaseReviewBuilder WithAssignedTo(string investigator)
    {
        _assignedTo = investigator;
        return this;
    }

    public CaseReviewBuilder WithStatus(string status)
    {
        _status = status;
        return this;
    }

    public CaseReviewBuilder WithPriority(string priority)
    {
        _priority = priority;
        return this;
    }

    public CaseReviewBuilder WithDecision(string decision, string justification)
    {
        _decision = decision;
        _justification = justification;
        return this;
    }

    public CaseReviewBuilder AsResolved()
    {
        _status = "Resolved";
        _resolvedAt = DateTime.UtcNow;
        return this;
    }

    public CaseReview Build()
    {
        return new CaseReview
        {
            CaseId = _caseId,
            ClaimId = _claimId,
            AssignedTo = _assignedTo,
            Status = _status,
            Priority = _priority,
            CreatedAt = _createdAt,
            ResolvedAt = _resolvedAt,
            Decision = _decision,
            Justification = _justification,
            History = []
        };
    }
}

/// <summary>
/// Test data builder for creating AppUser objects
/// </summary>
public class AppUserBuilder
{
    private string _userId = "user-default";
    private string _displayName = "Test User";
    private string _email = "testuser@example.com";
    private string _roleId = "role-user";
    private string _department = "Claims";
    private bool _isActive = true;

    public AppUserBuilder WithUserId(string userId)
    {
        _userId = userId;
        return this;
    }

    public AppUserBuilder WithDisplayName(string name)
    {
        _displayName = name;
        return this;
    }

    public AppUserBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public AppUserBuilder WithRole(string roleId)
    {
        _roleId = roleId;
        return this;
    }

    public AppUserBuilder WithDepartment(string department)
    {
        _department = department;
        return this;
    }

    public AppUserBuilder AsInactive()
    {
        _isActive = false;
        return this;
    }

    public AppUser Build()
    {
        return new AppUser
        {
            UserId = _userId,
            DisplayName = _displayName,
            Email = _email,
            RoleId = _roleId,
            Department = _department,
            IsActive = _isActive,
            CreatedAt = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Test data builder for creating ProviderProfile objects
/// </summary>
public class ProviderProfileBuilder
{
    private string _providerId = "PR-default";
    private string _providerName = "Test Provider";
    private string _specialty = "General";
    private string _location = "Test Location";
    private int _totalClaims = 100;
    private decimal _totalBilled = 500000m;
    private decimal _avgClaimAmount = 5000m;
    private decimal _peerAvgAmount = 4500m;
    private double _deviationPercent = 11.1;
    private int _flaggedClaims = 2;
    private double _flagRate = 2.0;
    private int _riskScore = 35;
    private string _riskLevel = "Low";

    public ProviderProfileBuilder WithProviderId(string providerId)
    {
        _providerId = providerId;
        return this;
    }

    public ProviderProfileBuilder WithProviderName(string name)
    {
        _providerName = name;
        return this;
    }

    public ProviderProfileBuilder WithRiskScore(int score)
    {
        _riskScore = score;
        _riskLevel = score switch
        {
            <= 40 => "Low",
            <= 70 => "Medium",
            _ => "High"
        };
        return this;
    }

    public ProviderProfileBuilder WithTotalClaims(int count)
    {
        _totalClaims = count;
        return this;
    }

    public ProviderProfileBuilder WithAverageClaimAmount(decimal amount)
    {
        _avgClaimAmount = amount;
        return this;
    }

    public ProviderProfile Build()
    {
        return new ProviderProfile
        {
            ProviderId = _providerId,
            ProviderName = _providerName,
            Specialty = _specialty,
            Location = _location,
            TotalClaims = _totalClaims,
            TotalBilled = _totalBilled,
            AvgClaimAmount = _avgClaimAmount,
            PeerAvgAmount = _peerAvgAmount,
            DeviationPercent = _deviationPercent,
            FlaggedClaims = _flaggedClaims,
            FlagRate = _flagRate,
            RiskScore = _riskScore,
            RiskLevel = _riskLevel,
            RiskIndicators = []
        };
    }
}
