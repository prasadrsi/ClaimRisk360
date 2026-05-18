using ClaimRisk360.Models;
using ClaimRisk360.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ClaimRisk360.Pages;

public class ClaimsModel : PageModel
{
    private readonly FraudDetectionService _service;
    private readonly ClaimApprovalService _approvalService;
    private readonly RoleService _roleService;

    public PaginatedResult<Claim> PaginatedClaims { get; set; } = new();
    public List<Claim> Claims { get; set; } = [];  // Legacy support
    public ApprovalSummary Summary { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public int PageSize { get; set; } = 50;

    [BindProperty(SupportsGet = true)]
    public string? Filter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? ApprovalFilter { get; set; }

    [BindProperty]
    public string? ApproveClaimId { get; set; }

    [BindProperty]
    public string? ApprovalComment { get; set; }

    [BindProperty]
    public string? ApprovalAction { get; set; }

    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    public ClaimsModel(FraudDetectionService service, ClaimApprovalService approvalService, RoleService roleService)
    {
        _service = service;
        _approvalService = approvalService;
        _roleService = roleService;
    }

    public async Task OnGetAsync(string? success)
    {
        if (success is not null) SuccessMessage = success;
        await LoadClaimsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(ApproveClaimId))
        {
            ErrorMessage = "No claim selected.";
            await LoadClaimsAsync();
            return Page();
        }

        var userName = _roleService.GetCurrentUser().DisplayName;
        string? error;

        if (ApprovalAction == "Reject")
            error = _approvalService.RejectClaim(ApproveClaimId, ApprovalComment ?? "", userName);
        else
            error = _approvalService.ApproveClaim(ApproveClaimId, ApprovalComment ?? "", userName);

        if (error is not null)
        {
            ErrorMessage = error;
            await LoadClaimsAsync();
            return Page();
        }

        return RedirectToPage(new { PageNumber, PageSize, Filter, ApprovalFilter, success = $"Claim {ApproveClaimId} {ApprovalAction?.ToLower()}d successfully" });
    }

    private async Task LoadClaimsAsync()
    {
        // Load paginated claims
        PaginatedClaims = await _service.GetClaimsPaginatedAsync(PageNumber, PageSize);
        Claims = PaginatedClaims.Items;  // For backward compatibility

        Summary = _approvalService.GetSummary();

        if (!string.IsNullOrEmpty(Filter) && Filter != "All")
            Claims = Claims.Where(c => c.RiskCategory == Filter).ToList();

        if (!string.IsNullOrEmpty(ApprovalFilter) && ApprovalFilter != "All")
            Claims = Claims.Where(c => c.ApprovalStatus == ApprovalFilter).ToList();
    }
}
