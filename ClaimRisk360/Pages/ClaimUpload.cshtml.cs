using ClaimRisk360.Models;
using ClaimRisk360.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ClaimRisk360.Pages;

public class ClaimUploadModel : PageModel
{
    private readonly ClaimValidationService _validationService;
    private readonly AuditService _auditService;
    private readonly DocumentService _documentService;

    [BindProperty]
    public ClaimUploadRequest ClaimRequest { get; set; } = new();

    [BindProperty]
    public List<IFormFile> Documents { get; set; } = [];

    [BindProperty]
    public List<string> DocumentTypes { get; set; } = [];

    public ValidationResult? Result { get; set; }
    public bool Submitted { get; set; }
    public string? GeneratedClaimId { get; set; }
    public List<ClaimDocument> UploadedDocuments { get; set; } = [];

    public ClaimUploadModel(ClaimValidationService validationService, AuditService auditService, DocumentService documentService)
    {
        _validationService = validationService;
        _auditService = auditService;
        _documentService = documentService;
    }

    public void OnGet()
    {
    }

    public void OnPost()
    {
        Submitted = true;
        Result = _validationService.Validate(ClaimRequest);

        GeneratedClaimId = $"CLM-{DateTime.Now:yyyyMMddHHmmss}";
        var userName = User.Identity?.Name ?? "Unknown";

        _auditService.Log(GeneratedClaimId, "Claim Submitted", userName, "Claim submitted via web portal", "Ingestion");

        if (Result.IsValid)
        {
            _auditService.Log(GeneratedClaimId, "Validation Passed", "System", "All schema and business rules passed", "Validation");

            // Process uploaded documents
            for (int i = 0; i < Documents.Count; i++)
            {
                var file = Documents[i];
                var docType = i < DocumentTypes.Count && !string.IsNullOrEmpty(DocumentTypes[i])
                    ? DocumentTypes[i]
                    : "Other";

                _documentService.AddDocument(GeneratedClaimId, file.FileName, docType, file.Length, userName);
            }

            UploadedDocuments = _documentService.GetByClaimId(GeneratedClaimId);
        }
        else
        {
            _auditService.Log(GeneratedClaimId, "Validation Failed", "System",
                $"Rejected with {Result.Errors.Count} error(s): {string.Join(", ", Result.Errors.Select(e => e.Code))}", "Validation");
        }
    }
}
