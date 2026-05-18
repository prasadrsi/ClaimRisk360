using ClaimRisk360.Models;
using ClaimRisk360.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ClaimRisk360.Pages;

public class DashboardModel : PageModel
{
    private readonly FraudDetectionService _service;
    public DashboardStats Stats { get; set; } = new();

    public DashboardModel(FraudDetectionService service) => _service = service;

    // Async handler for better performance
    public async Task OnGetAsync() => Stats = await _service.GetDashboardStatsAsync();
}
