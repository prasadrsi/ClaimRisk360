using ClaimRisk360.Models;
using ClaimRisk360.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ClaimRisk360.Pages;

public class UserManagementModel : PageModel
{
    private readonly RoleService _roleService;

    public List<AppUser> Users { get; set; } = [];
    public List<AppRole> Roles { get; set; } = [];
    public AppUser CurrentUser { get; set; } = new();
    public AppRole CurrentRole { get; set; } = new();
    public RolePermissions Permissions { get; set; } = new();

    // Edit user
    public string? SelectedUserId { get; set; }
    public AppUser? SelectedUser { get; set; }

    [BindProperty] public string? EditUserId { get; set; }
    [BindProperty] public string? EditRoleId { get; set; }
    [BindProperty] public string? EditDepartment { get; set; }
    [BindProperty] public bool EditIsActive { get; set; }

    // Add user
    [BindProperty] public string? NewDisplayName { get; set; }
    [BindProperty] public string? NewEmail { get; set; }
    [BindProperty] public string? NewRoleId { get; set; }
    [BindProperty] public string? NewDepartment { get; set; }

    // Switch role
    [BindProperty] public string? SwitchToUserId { get; set; }

    public string? SuccessMessage { get; set; }

    public UserManagementModel(RoleService roleService)
    {
        _roleService = roleService;
    }

    public void OnGet(string? userId, string? success)
    {
        LoadData();
        SelectedUserId = userId;
        if (userId is not null)
            SelectedUser = _roleService.GetUser(userId);
        if (success is not null)
            SuccessMessage = success;
    }

    public IActionResult OnPostSwitchRole()
    {
        if (!string.IsNullOrEmpty(SwitchToUserId))
            _roleService.SwitchUser(SwitchToUserId);

        return RedirectToPage(new { success = "Role switched successfully" });
    }

    public IActionResult OnPostUpdateUser()
    {
        if (!string.IsNullOrEmpty(EditUserId) && !string.IsNullOrEmpty(EditRoleId))
        {
            _roleService.UpdateUser(EditUserId, EditRoleId, EditDepartment ?? "",
                EditIsActive, _roleService.GetCurrentUser().DisplayName);
        }
        return RedirectToPage(new { userId = EditUserId, success = "User updated" });
    }

    public IActionResult OnPostAddUser()
    {
        if (!string.IsNullOrEmpty(NewDisplayName) && !string.IsNullOrEmpty(NewEmail) && !string.IsNullOrEmpty(NewRoleId))
        {
            _roleService.AddUser(NewDisplayName, NewEmail, NewRoleId,
                NewDepartment ?? "", _roleService.GetCurrentUser().DisplayName);
        }
        return RedirectToPage(new { success = "User created" });
    }

    private void LoadData()
    {
        Users = _roleService.GetAllUsers();
        Roles = _roleService.GetAllRoles();
        CurrentUser = _roleService.GetCurrentUser();
        CurrentRole = _roleService.GetCurrentRole();
        Permissions = _roleService.GetCurrentPermissions();
    }
}
