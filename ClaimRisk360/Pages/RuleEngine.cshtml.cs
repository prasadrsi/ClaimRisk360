using ClaimRisk360.Models;
using ClaimRisk360.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ClaimRisk360.Pages;

public class RuleEngineModel : PageModel
{
    private readonly RuleEngineService _ruleService;

    public List<RuleCheckResult> AllResults { get; set; } = [];
    public List<RuleCheckResult> TriggeredResults { get; set; } = [];

    [BindProperty(SupportsGet = true)]
    public string? FilterCategory { get; set; }

    public RuleEngineModel(RuleEngineService ruleService) => _ruleService = ruleService;

    public async Task OnGetAsync(string? category)
    {
        FilterCategory = category;
        AllResults = await _ruleService.RunAllRulesAsync();

        if (!string.IsNullOrEmpty(category) && category != "All")
            AllResults = AllResults.Where(r => r.Category == category).ToList();

        TriggeredResults = AllResults.Where(r => r.Triggered).ToList();
    }
}
