using ClaimRisk360.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ClaimRisk360.Pages;

public class RulesDocumentationModel : PageModel
{
    private readonly ReferenceDataRepository _refData;

    public HashSet<string> DiagnosisCodes { get; set; } = [];
    public HashSet<string> ProcedureCodes { get; set; } = [];
    public HashSet<string> ActiveProviders { get; set; } = [];
    public HashSet<string> BlacklistedProviders { get; set; } = [];
    public HashSet<string> BlacklistedPatients { get; set; } = [];
    public HashSet<string> WatchlistAccounts { get; set; } = [];

    public RulesDocumentationModel(ReferenceDataRepository refData)
    {
        _refData = refData;
    }

    public void OnGet()
    {
        DiagnosisCodes = _refData.ValidDiagnosisCodes;
        ProcedureCodes = _refData.ValidProcedureCodes;
        ActiveProviders = _refData.ActiveProviders;
        BlacklistedProviders = _refData.BlacklistedProviders;
        BlacklistedPatients = _refData.BlacklistedPatients;
        WatchlistAccounts = _refData.WatchlistBankAccounts;
    }
}
