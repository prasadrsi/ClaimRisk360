using ClaimRisk360.Models;
using ClaimRisk360.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ClaimRisk360.Pages;

public class AuditTrailModel : PageModel
{
    private readonly AuditService _auditService;

    public PaginatedResult<AuditEntry> PaginatedEntries { get; set; } = new();
    public List<AuditEntry> Entries { get; set; } = [];

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public int PageSize { get; set; } = 50;

    [BindProperty(SupportsGet = true)]
    public string? FilterCategory { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? FilterClaimId { get; set; }

    public AuditTrailModel(AuditService auditService)
    {
        _auditService = auditService;
    }

    public async Task OnGetAsync(string? category, string? claimId)
    {
        FilterCategory = category;
        FilterClaimId = claimId;

        PaginatedEntries = await _auditService.GetPaginatedAsync(PageNumber, PageSize);
        Entries = PaginatedEntries.Items;

        if (!string.IsNullOrEmpty(category) && category != "All")
            Entries = Entries.Where(e => e.Category == category).ToList();

        if (!string.IsNullOrEmpty(claimId))
            Entries = Entries.Where(e => e.ClaimId == claimId).ToList();
    }
}
