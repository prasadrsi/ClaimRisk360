using ClaimRisk360.Models;
using ClaimRisk360.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ClaimRisk360.Pages;

public class PatternAnalysisModel : PageModel
{
    private readonly PatternAnalysisService _patternService;

    public List<ClaimPattern> Patterns { get; set; } = [];

    [BindProperty(SupportsGet = true)]
    public string? FilterType { get; set; }

    public PatternAnalysisModel(PatternAnalysisService patternService) => _patternService = patternService;

    public async Task OnGetAsync(string? type)
    {
        FilterType = type;
        Patterns = await _patternService.DetectPatternsAsync();

        if (!string.IsNullOrEmpty(type) && type != "All")
            Patterns = Patterns.Where(p => p.PatternType == type).ToList();
    }
}
