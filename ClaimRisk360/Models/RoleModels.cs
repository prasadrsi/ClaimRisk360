using System.ComponentModel.DataAnnotations;

namespace ClaimRisk360.Models;

public class AppUser
{
    [Key]
    public string UserId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string RoleId { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLogin { get; set; }
}

public class AppRole
{
    [Key]
    public string RoleId { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string BadgeClass { get; set; } = "bg-secondary";
    public string IconClass { get; set; } = "bi-person";
    public RolePermissions Permissions { get; set; } = new();
}

public class RolePermissions
{
    // Dashboard
    public bool CanViewDashboard { get; set; }

    // Claims
    public bool CanSubmitClaim { get; set; }
    public bool CanViewClaims { get; set; }
    public bool CanReviewClaim { get; set; }

    // Fraud Detection
    public bool CanViewFraudAlerts { get; set; }
    public bool CanViewPatterns { get; set; }
    public bool CanViewMlModels { get; set; }

    // Investigation
    public bool CanManageCases { get; set; }
    public bool CanApproveClaim { get; set; }
    public bool CanRejectClaim { get; set; }
    public bool CanEscalateClaim { get; set; }
    public bool CanViewFraudRings { get; set; }
    public bool CanViewProviderProfiles { get; set; }

    // Reporting
    public bool CanViewReports { get; set; }
    public bool CanExportReports { get; set; }

    // Administration
    public bool CanViewAuditTrail { get; set; }
    public bool CanViewEthicsReport { get; set; }
    public bool CanManageUsers { get; set; }
    public bool CanManageRoles { get; set; }
    public bool CanConfigureSystem { get; set; }
}
