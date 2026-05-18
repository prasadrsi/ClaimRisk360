namespace ClaimRisk360.Data;

/// <summary>
/// Data layer: loads reference/configuration data.
/// These are static lookup values — kept in-memory (no DB table needed).
/// </summary>
public class ReferenceDataRepository
{
    public HashSet<string> ValidDiagnosisCodes { get; } =
        ["I25.1", "M54.5", "J06.9", "E11.9", "K21.0", "L30.9", "G43.9", "S82.0", "I10", "J18.9", "N39.0", "R10.9"];

    public HashSet<string> ValidProcedureCodes { get; } =
        ["99213", "99214", "99215", "99223", "99232", "99291", "36415", "71046", "80053", "85025"];

    public HashSet<string> ActiveProviders { get; } =
        ["PRV-100", "PRV-101", "PRV-102", "PRV-103", "PRV-104", "PRV-105", "PRV-106", "PRV-107", "PRV-108"];

    public HashSet<string> BlacklistedProviders { get; } = ["PRV-999", "PRV-888"];
    public HashSet<string> BlacklistedPatients { get; } = ["PAT-9999"];
    public HashSet<string> WatchlistBankAccounts { get; } = ["ACCT-XXXX-7777", "ACCT-XXXX-8888"];
    public List<string> Investigators { get; } = ["Sarah Chen", "James Rivera", "Priya Sharma", "Marcus Johnson"];
}
