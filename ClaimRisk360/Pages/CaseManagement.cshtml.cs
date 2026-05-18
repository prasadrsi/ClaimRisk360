using ClaimRisk360.Models;
using ClaimRisk360.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ClaimRisk360.Pages;

public class CaseManagementModel : PageModel
{
    private readonly CaseManagementService _caseService;
    private readonly FraudDetectionService _fraudService;
    private readonly DocumentService _documentService;
    private readonly RoleService _roleService;

    public List<CaseReview> Cases { get; set; } = [];
    public CaseReview? SelectedCase { get; set; }
    public Claim? RelatedClaim { get; set; }
    public List<ClaimDocument> Documents { get; set; } = [];

    [BindProperty(SupportsGet = true)]
    public string? SelectedCaseId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? StatusFilter { get; set; }

    [BindProperty]
    public string? Decision { get; set; }

    [BindProperty]
    public string? Justification { get; set; }

    public string? ErrorMessage { get; set; }

    public CaseManagementModel(CaseManagementService caseService, FraudDetectionService fraudService,
        DocumentService documentService, RoleService roleService)
    {
        _caseService = caseService;
        _fraudService = fraudService;
        _documentService = documentService;
        _roleService = roleService;
    }

    public void OnGet()
    {
        LoadCases();

        if (!string.IsNullOrEmpty(SelectedCaseId))
        {
            SelectedCase = _caseService.GetCase(SelectedCaseId);
            if (SelectedCase is not null)
            {
                RelatedClaim = _fraudService.GetClaim(SelectedCase.ClaimId);
                Documents = _documentService.GetByClaimId(SelectedCase.ClaimId);
            }
        }
    }

    public IActionResult OnPost()
    {
        if (string.IsNullOrWhiteSpace(Justification))
        {
            ErrorMessage = "Comment is mandatory for all decisions.";
            LoadCases();
            if (!string.IsNullOrEmpty(SelectedCaseId))
            {
                SelectedCase = _caseService.GetCase(SelectedCaseId);
                if (SelectedCase is not null)
                {
                    RelatedClaim = _fraudService.GetClaim(SelectedCase.ClaimId);
                    Documents = _documentService.GetByClaimId(SelectedCase.ClaimId);
                }
            }
            return Page();
        }

        if (!string.IsNullOrEmpty(SelectedCaseId) && !string.IsNullOrEmpty(Decision))
        {
            var userName = _roleService.GetCurrentUser().DisplayName;
            var error = _caseService.UpdateDecision(SelectedCaseId, Decision, Justification, userName);
            if (error is not null)
            {
                ErrorMessage = error;
                LoadCases();
                SelectedCase = _caseService.GetCase(SelectedCaseId);
                if (SelectedCase is not null)
                {
                    RelatedClaim = _fraudService.GetClaim(SelectedCase.ClaimId);
                    Documents = _documentService.GetByClaimId(SelectedCase.ClaimId);
                }
                return Page();
            }
        }

        return RedirectToPage(new { SelectedCaseId, StatusFilter });
    }

    private void LoadCases()
    {
        Cases = _caseService.GetAll();
        if (!string.IsNullOrEmpty(StatusFilter) && StatusFilter != "All")
            Cases = Cases.Where(c => c.Status == StatusFilter).ToList();
    }
}
