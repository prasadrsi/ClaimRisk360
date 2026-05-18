using ClaimRisk360.Models;
using ClaimRisk360.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ClaimRisk360.Pages;

public class ProviderProfilingModel : PageModel
{
    private readonly ProviderProfileService _profileService;
    private readonly FraudDetectionService _fraudService;

    public List<ProviderProfile> Profiles { get; set; } = [];
    public ProviderProfile? SelectedProfile { get; set; }
    public List<Claim> ProviderClaims { get; set; } = [];
    public string? SelectedProviderId { get; set; }

    public ProviderProfilingModel(ProviderProfileService profileService, FraudDetectionService fraudService)
    {
        _profileService = profileService;
        _fraudService = fraudService;
    }

    public void OnGet(string? providerId)
    {
        SelectedProviderId = providerId;
        Profiles = _profileService.GetAllProfiles();

        if (!string.IsNullOrEmpty(providerId))
        {
            SelectedProfile = _profileService.GetProfile(providerId);
            ProviderClaims = _fraudService.GetAllClaims().Where(c => c.ProviderId == providerId).ToList();
        }
    }
}
