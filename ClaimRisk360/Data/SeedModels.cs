namespace ClaimRisk360.Data;

/// <summary>
/// Reference/configuration data loaded from JSON.
/// </summary>
public class ReferenceData
{
    public List<string> ValidDiagnosisCodes { get; set; } = [];
    public List<string> ValidProcedureCodes { get; set; } = [];
    public List<string> ActiveProviders { get; set; } = [];
    public List<string> BlacklistedProviders { get; set; } = [];
    public List<string> BlacklistedPatients { get; set; } = [];
    public List<string> WatchlistBankAccounts { get; set; } = [];
    public List<string> Investigators { get; set; } = [];
}

/// <summary>
/// DTO for audit entry seed data (with DaysAgo instead of absolute timestamp).
/// </summary>
public class AuditEntrySeed
{
    public string ClaimId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string PerformedBy { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int DaysAgo { get; set; }
}

/// <summary>
/// DTO for document seed data (with DaysAgo instead of absolute timestamp).
/// </summary>
public class DocumentSeed
{
    public string ClaimId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string UploadedBy { get; set; } = string.Empty;
    public int DaysAgo { get; set; }
}
