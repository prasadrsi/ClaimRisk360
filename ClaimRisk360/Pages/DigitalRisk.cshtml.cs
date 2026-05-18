using ClaimRisk360.Models;
using ClaimRisk360.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ClaimRisk360.Pages;

public class DigitalRiskModel : PageModel
{
    private readonly DigitalRiskService _digitalRiskService;

    public List<DigitalRiskSignal> Signals { get; set; } = [];
    public List<StpDecision> StpDecisions { get; set; } = [];
    public string? FilterType { get; set; }
    public string ActiveTab { get; set; } = "signals";

    public DigitalRiskModel(DigitalRiskService digitalRiskService) => _digitalRiskService = digitalRiskService;

    public void OnGet(string? type, string? tab)
    {
        ActiveTab = tab ?? "signals";
        FilterType = type;
        Signals = _digitalRiskService.GetAllSignals();
        StpDecisions = _digitalRiskService.GetStpDecisions();

        if (!string.IsNullOrEmpty(type) && type != "All")
            Signals = Signals.Where(s => s.SignalType == type).ToList();
    }
}
