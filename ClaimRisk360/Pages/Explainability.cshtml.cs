using ClaimRisk360.Models;
using ClaimRisk360.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ClaimRisk360.Pages;

public class ExplainabilityModel : PageModel
{
    private readonly FraudDetectionService _service;
    private readonly DocumentService _documentService;

    [BindProperty(SupportsGet = true)]
    public string ClaimId { get; set; } = string.Empty;

    public ExplainabilityResult Result { get; set; } = new();
    public Claim? Claim { get; set; }
    public List<ClaimDocument> Documents { get; set; } = [];

    public ExplainabilityModel(FraudDetectionService service, DocumentService documentService)
    {
        _service = service;
        _documentService = documentService;
    }

    public IActionResult OnGet()
    {
        if (string.IsNullOrEmpty(ClaimId))
        {
            var first = _service.GetAllClaims().FirstOrDefault();
            if (first is not null) ClaimId = first.ClaimId;
        }

        if (string.IsNullOrEmpty(ClaimId))
            return Page();

        Claim = _service.GetClaim(ClaimId);
        if (Claim is not null)
        {
            Result = _service.GetExplainability(ClaimId);
            Documents = _documentService.GetByClaimId(ClaimId);
        }
        return Page();
    }
}
