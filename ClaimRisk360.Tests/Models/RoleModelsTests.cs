using ClaimRisk360.Models;
using FluentAssertions;
using Xunit;

namespace ClaimRisk360.Tests.Models;

public class AppUserTests
{
    [Fact]
    public void AppUser_DefaultConstructor_InitializesWithDefaults()
    {
        // Arrange & Act
        var user = new AppUser();

        // Assert
        user.UserId.Should().Be(string.Empty);
        user.DisplayName.Should().Be(string.Empty);
        user.Email.Should().Be(string.Empty);
        user.RoleId.Should().Be(string.Empty);
        user.Department.Should().Be(string.Empty);
        user.IsActive.Should().BeTrue();
        user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        user.LastLogin.Should().BeNull();
    }

    [Fact]
    public void AppUser_SetProperties_UpdatesCorrectly()
    {
        // Arrange
        var user = new AppUser();
        var userId = "user123";
        var displayName = "John Doe";
        var email = "john@example.com";
        var roleId = "role456";
        var department = "Claims";

        // Act
        user.UserId = userId;
        user.DisplayName = displayName;
        user.Email = email;
        user.RoleId = roleId;
        user.Department = department;

        // Assert
        user.UserId.Should().Be(userId);
        user.DisplayName.Should().Be(displayName);
        user.Email.Should().Be(email);
        user.RoleId.Should().Be(roleId);
        user.Department.Should().Be(department);
    }

    [Fact]
    public void AppUser_SetLastLogin_UpdatesCorrectly()
    {
        // Arrange
        var user = new AppUser();
        var lastLogin = DateTime.UtcNow;

        // Act
        user.LastLogin = lastLogin;

        // Assert
        user.LastLogin.Should().Be(lastLogin);
    }

    [Fact]
    public void AppUser_SetIsActiveToFalse_UpdatesCorrectly()
    {
        // Arrange
        var user = new AppUser { IsActive = true };

        // Act
        user.IsActive = false;

        // Assert
        user.IsActive.Should().BeFalse();
    }

    [Theory]
    [InlineData("user1@example.com")]
    [InlineData("user2@example.com")]
    [InlineData("user3@example.com")]
    public void AppUser_MultipleEmails_AllValid(string email)
    {
        // Arrange & Act
        var user = new AppUser { Email = email };

        // Assert
        user.Email.Should().Be(email);
    }
}

public class AppRoleTests
{
    [Fact]
    public void AppRole_DefaultConstructor_InitializesWithDefaults()
    {
        // Arrange & Act
        var role = new AppRole();

        // Assert
        role.RoleId.Should().Be(string.Empty);
        role.RoleName.Should().Be(string.Empty);
        role.Description.Should().Be(string.Empty);
        role.BadgeClass.Should().Be("bg-secondary");
        role.IconClass.Should().Be("bi-person");
        role.Permissions.Should().NotBeNull();
    }

    [Fact]
    public void AppRole_SetProperties_UpdatesCorrectly()
    {
        // Arrange
        var role = new AppRole();

        // Act
        role.RoleId = "admin";
        role.RoleName = "Administrator";
        role.Description = "System Administrator";
        role.BadgeClass = "bg-danger";
        role.IconClass = "bi-shield-check";

        // Assert
        role.RoleId.Should().Be("admin");
        role.RoleName.Should().Be("Administrator");
        role.Description.Should().Be("System Administrator");
        role.BadgeClass.Should().Be("bg-danger");
        role.IconClass.Should().Be("bi-shield-check");
    }

    [Theory]
    [InlineData("admin", "bg-danger")]
    [InlineData("user", "bg-info")]
    [InlineData("moderator", "bg-warning")]
    public void AppRole_DifferentBadgeClasses_UpdateCorrectly(string roleId, string badgeClass)
    {
        // Arrange & Act
        var role = new AppRole { RoleId = roleId, BadgeClass = badgeClass };

        // Assert
        role.RoleId.Should().Be(roleId);
        role.BadgeClass.Should().Be(badgeClass);
    }
}

public class RolePermissionsTests
{
    [Fact]
    public void RolePermissions_DefaultConstructor_AllPermissionsFalse()
    {
        // Arrange & Act
        var permissions = new RolePermissions();

        // Assert
        permissions.CanViewDashboard.Should().BeFalse();
        permissions.CanSubmitClaim.Should().BeFalse();
        permissions.CanViewClaims.Should().BeFalse();
        permissions.CanReviewClaim.Should().BeFalse();
        permissions.CanViewFraudAlerts.Should().BeFalse();
        permissions.CanViewPatterns.Should().BeFalse();
        permissions.CanViewMlModels.Should().BeFalse();
        permissions.CanManageCases.Should().BeFalse();
        permissions.CanApproveClaim.Should().BeFalse();
        permissions.CanRejectClaim.Should().BeFalse();
        permissions.CanEscalateClaim.Should().BeFalse();
        permissions.CanViewFraudRings.Should().BeFalse();
        permissions.CanViewProviderProfiles.Should().BeFalse();
        permissions.CanViewReports.Should().BeFalse();
        permissions.CanExportReports.Should().BeFalse();
        permissions.CanViewAuditTrail.Should().BeFalse();
        permissions.CanViewEthicsReport.Should().BeFalse();
        permissions.CanManageUsers.Should().BeFalse();
        permissions.CanManageRoles.Should().BeFalse();
        permissions.CanConfigureSystem.Should().BeFalse();
    }

    [Fact]
    public void RolePermissions_GrantPermissions_UpdatesCorrectly()
    {
        // Arrange
        var permissions = new RolePermissions();

        // Act
        permissions.CanViewDashboard = true;
        permissions.CanSubmitClaim = true;
        permissions.CanViewClaims = true;

        // Assert
        permissions.CanViewDashboard.Should().BeTrue();
        permissions.CanSubmitClaim.Should().BeTrue();
        permissions.CanViewClaims.Should().BeTrue();
    }

    [Fact]
    public void RolePermissions_AdminRole_HasAllPermissions()
    {
        // Arrange
        var permissions = new RolePermissions();

        // Act - Grant all admin permissions
        permissions.CanViewDashboard = true;
        permissions.CanSubmitClaim = true;
        permissions.CanViewClaims = true;
        permissions.CanReviewClaim = true;
        permissions.CanViewFraudAlerts = true;
        permissions.CanViewPatterns = true;
        permissions.CanViewMlModels = true;
        permissions.CanManageCases = true;
        permissions.CanApproveClaim = true;
        permissions.CanRejectClaim = true;
        permissions.CanEscalateClaim = true;
        permissions.CanViewFraudRings = true;
        permissions.CanViewProviderProfiles = true;
        permissions.CanViewReports = true;
        permissions.CanExportReports = true;
        permissions.CanViewAuditTrail = true;
        permissions.CanViewEthicsReport = true;
        permissions.CanManageUsers = true;
        permissions.CanManageRoles = true;
        permissions.CanConfigureSystem = true;

        // Assert
        permissions.CanViewDashboard.Should().BeTrue();
        permissions.CanManageUsers.Should().BeTrue();
        permissions.CanConfigureSystem.Should().BeTrue();
    }

    [Fact]
    public void RolePermissions_LimitedUserRole_OnlyViewPermissions()
    {
        // Arrange
        var permissions = new RolePermissions();

        // Act - Grant only view permissions
        permissions.CanViewDashboard = true;
        permissions.CanViewClaims = true;
        permissions.CanViewReports = true;

        // Assert
        permissions.CanViewDashboard.Should().BeTrue();
        permissions.CanViewClaims.Should().BeTrue();
        permissions.CanViewReports.Should().BeTrue();
        permissions.CanSubmitClaim.Should().BeFalse();
        permissions.CanApproveClaim.Should().BeFalse();
        permissions.CanManageUsers.Should().BeFalse();
    }
}
