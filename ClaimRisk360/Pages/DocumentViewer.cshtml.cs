using ClaimRisk360.Models;
using ClaimRisk360.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ClaimRisk360.Pages;

public class DocumentViewerModel : PageModel
{
    private readonly DocumentService _documentService;
    private readonly FraudDetectionService _fraudService;
    private readonly AuditService _auditService;
    private readonly RoleService _roleService;

    [BindProperty(SupportsGet = true)]
    public string DocumentId { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string? ClaimId { get; set; }

    public ClaimDocument? Document { get; set; }
    public Claim? Claim { get; set; }
    public List<ClaimDocument> RelatedDocuments { get; set; } = [];

    public DocumentViewerModel(DocumentService documentService, FraudDetectionService fraudService,
        AuditService auditService, RoleService roleService)
    {
        _documentService = documentService;
        _fraudService = fraudService;
        _auditService = auditService;
        _roleService = roleService;
    }

    public IActionResult OnGet()
    {
        if (string.IsNullOrEmpty(DocumentId))
            return RedirectToPage("/Claims");

        Document = _documentService.GetDocument(DocumentId);
        if (Document is null)
            return RedirectToPage("/Claims");

        Claim = _fraudService.GetClaim(Document.ClaimId);
        RelatedDocuments = _documentService.GetByClaimId(Document.ClaimId);

        // Log document view for audit trail
        var userName = _roleService.GetCurrentUser().DisplayName;
        _auditService.Log(Document.ClaimId, "Document Viewed", userName,
            $"Viewed {Document.DocumentType}: {Document.FileName} ({Document.DocumentId})", "Audit");

        return Page();
    }
}
