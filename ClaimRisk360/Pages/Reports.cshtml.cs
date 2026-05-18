using ClaimRisk360.Models;
using ClaimRisk360.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ClaimRisk360.Pages;

public class ReportsModel : PageModel
{
    private readonly FraudDetectionService _fraudService;
    private readonly CaseManagementService _caseService;
    private readonly RuleEngineService _ruleService;
    private readonly DigitalRiskService _digitalRiskService;

    public DashboardStats Stats { get; set; } = new();
    public int TotalCases { get; set; }
    public int ResolvedCases { get; set; }
    public int EscalatedCases { get; set; }
    public int OpenCases { get; set; }
    public int TriggeredRules { get; set; }
    public int AutoApproved { get; set; }
    public int AutoRejected { get; set; }
    public int RoutedToReview { get; set; }
    public List<Claim> TopRiskClaims { get; set; } = [];

    public ReportsModel(FraudDetectionService fraudService, CaseManagementService caseService,
        RuleEngineService ruleService, DigitalRiskService digitalRiskService)
    {
        _fraudService = fraudService;
        _caseService = caseService;
        _ruleService = ruleService;
        _digitalRiskService = digitalRiskService;
    }

    public async Task OnGetAsync()
    {
        Stats = await _fraudService.GetDashboardStatsAsync();

        var cases = _caseService.GetAll();
        TotalCases = cases.Count;
        ResolvedCases = cases.Count(c => c.Status == "Resolved");
        EscalatedCases = cases.Count(c => c.Status == "Escalated");
        OpenCases = cases.Count(c => c.Status is "Open" or "In Review");

        TriggeredRules = (await _ruleService.GetTriggeredRulesAsync()).Count;

        var stpDecisions = _digitalRiskService.GetStpDecisions();
        AutoApproved = stpDecisions.Count(s => s.Action == "Auto-Approved");
        AutoRejected = stpDecisions.Count(s => s.Action == "Auto-Rejected");
        RoutedToReview = stpDecisions.Count(s => s.Action == "Routed to Review");

        var allClaims = await _fraudService.GetAllClaimsAsync();
        TopRiskClaims = allClaims
            .Where(c => c.FraudRiskScore > 70)
            .OrderByDescending(c => c.FraudRiskScore)
            .Take(10)
            .ToList();
    }
}
