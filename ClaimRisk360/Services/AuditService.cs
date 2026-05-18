using ClaimRisk360.Data;
using ClaimRisk360.Models;

namespace ClaimRisk360.Services;

/// <summary>
/// Business Logic: audit logging operations.
/// Data persistence is handled by AuditRepository (Data Layer).
/// Includes async methods for performance optimization.
/// </summary>
public class AuditService
{
    private readonly AuditRepository _repo;

    public AuditService(AuditRepository repo)
    {
        _repo = repo;
    }

    #region Synchronous Methods (Legacy)
    public void Log(string claimId, string action, string performedBy, string details, string category = "System")
    {
        _repo.Add(new AuditEntry
        {
            ClaimId = claimId,
            Action = action,
            PerformedBy = performedBy,
            Timestamp = DateTime.UtcNow,
            Details = details,
            Category = category
        });
    }

    public List<AuditEntry> GetAll() => _repo.GetAll();

    public List<AuditEntry> GetByClaimId(string claimId) => _repo.GetByClaimId(claimId);
    #endregion

    #region Async Methods (New - Performance Optimized)

    /// <summary>
    /// Log audit entry asynchronously
    /// </summary>
    public async Task LogAsync(string claimId, string action, string performedBy, string details, string category = "System")
    {
        await _repo.AddAsync(new AuditEntry
        {
            ClaimId = claimId,
            Action = action,
            PerformedBy = performedBy,
            Timestamp = DateTime.UtcNow,
            Details = details,
            Category = category
        });
    }

    /// <summary>
    /// Get all audit entries asynchronously
    /// </summary>
    public async Task<List<AuditEntry>> GetAllAsync() => 
        await _repo.GetAllAsync();

    /// <summary>
    /// Get paginated audit entries asynchronously
    /// </summary>
    public async Task<PaginatedResult<AuditEntry>> GetPaginatedAsync(int pageNumber = 1, int pageSize = 50) =>
        await _repo.GetAllPaginatedAsync(pageNumber, pageSize);

    /// <summary>
    /// Get audit entries by claim ID asynchronously
    /// </summary>
    public async Task<List<AuditEntry>> GetByClaimIdAsync(string claimId) => 
        await _repo.GetByClaimIdAsync(claimId);

    #endregion
}
