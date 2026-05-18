using ClaimRisk360.Data;
using ClaimRisk360.Models;

namespace ClaimRisk360.Services;

/// <summary>
/// Business Logic: role-based access control.
/// Resolves the current user's role and permissions.
/// Supports session-based role switching for demo/dev.
/// </summary>
public class RoleService
{
    private readonly UserRepository _userRepo;
    private readonly AuditService _auditService;
    private readonly NotificationService _notificationService;

    // Session-based active role (for demo — in production this comes from claims/AD)
    private static string _activeUserId = "USR-004"; // Default: Admin

    public RoleService(UserRepository userRepo, AuditService auditService, NotificationService notificationService)
    {
        _userRepo = userRepo;
        _auditService = auditService;
        _notificationService = notificationService;
    }

    public AppUser GetCurrentUser() =>
        _userRepo.GetUser(_activeUserId) ?? _userRepo.GetAllUsers().First();

    public AppRole GetCurrentRole()
    {
        var user = GetCurrentUser();
        return _userRepo.GetRole(user.RoleId) ?? _userRepo.GetAllRoles().First();
    }

    public RolePermissions GetCurrentPermissions() => GetCurrentRole().Permissions;

    public void SwitchUser(string userId)
    {
        var user = _userRepo.GetUser(userId);
        if (user is null || !user.IsActive) return;

        var previousUser = GetCurrentUser();
        _activeUserId = userId;

        _auditService.Log("SYSTEM", "Role Switch",
            previousUser.DisplayName,
            $"Switched active user from {previousUser.DisplayName} ({previousUser.RoleId}) to {user.DisplayName} ({user.RoleId})",
            "Administration");

        _ = _notificationService.SendNotification(
            "Role Switched",
            $"Now operating as {user.DisplayName} ({user.RoleId})",
            "info");
        _ = _notificationService.SendDataRefresh("role");
    }

    // Convenience permission checks
    public bool Can(Func<RolePermissions, bool> check) => check(GetCurrentPermissions());

    // User management
    public List<AppUser> GetAllUsers() => _userRepo.GetAllUsers();
    public List<AppRole> GetAllRoles() => _userRepo.GetAllRoles();
    public AppUser? GetUser(string userId) => _userRepo.GetUser(userId);

    public void UpdateUser(string userId, string roleId, string department, bool isActive, string performedBy)
    {
        var user = _userRepo.GetUser(userId);
        if (user is null) return;

        var oldRole = user.RoleId;
        _userRepo.UpdateUser(userId, roleId, department, isActive);

        _auditService.Log("SYSTEM", "User Updated", performedBy,
            $"User {user.DisplayName}: role {oldRole} ? {roleId}, active={isActive}",
            "Administration");
    }

    public void AddUser(string displayName, string email, string roleId, string department, string performedBy)
    {
        var user = new AppUser
        {
            DisplayName = displayName,
            Email = email,
            RoleId = roleId,
            Department = department,
            IsActive = true
        };
        _userRepo.AddUser(user);

        _auditService.Log("SYSTEM", "User Created", performedBy,
            $"New user {displayName} ({email}) with role {roleId}",
            "Administration");
    }
}
