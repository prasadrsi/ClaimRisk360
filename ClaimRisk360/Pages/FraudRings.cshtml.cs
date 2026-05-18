using ClaimRisk360.Models;
using ClaimRisk360.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ClaimRisk360.Pages;

public class FraudRingsModel : PageModel
{
    private readonly FraudDetectionService _service;
    public List<FraudRing> Rings { get; set; } = [];

    [BindProperty(SupportsGet = true)]
    public string? SelectedRingId { get; set; }

    public FraudRing? SelectedRing { get; set; }

    public FraudRingsModel(FraudDetectionService service) => _service = service;

    public async Task OnGetAsync()
    {
        Rings = await _service.GetFraudRingsAsync();
        SelectedRing = !string.IsNullOrEmpty(SelectedRingId)
            ? await _service.GetFraudRingAsync(SelectedRingId)
            : Rings.FirstOrDefault();
        SelectedRingId = SelectedRing?.RingId;
    }
}
