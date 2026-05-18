using ClaimRisk360.Models;
using Microsoft.EntityFrameworkCore;

namespace ClaimRisk360.Data;

/// <summary>
/// Data layer: loads and stores audit entries from SQLite via EF Core.
/// Includes async methods for performance optimization.
/// </summary>
public class AuditRepository
{
    private readonly AppDbContext _db;
    private int _counter;

    public AuditRepository(AppDbContext db)
    {
        _db = db;
        _counter = _db.AuditEntries.Count();
    }

    #region Synchronous Methods (Legacy)
    public List<AuditEntry> GetAll() =>
        _db.AuditEntries
           .Where(a => a.CaseReviewId == null)
           .OrderByDescending(a => a.Timestamp)
           .ToList();

    public List<AuditEntry> GetByClaimId(string claimId) =>
        _db.AuditEntries
           .Where(a => a.ClaimId == claimId)
           .OrderByDescending(a => a.Timestamp)
           .ToList();

    public void Add(AuditEntry entry)
    {
        entry.AuditId = $"AUD-{Interlocked.Increment(ref _counter):D5}";
        _db.AuditEntries.Add(entry);
        _db.SaveChanges();
    }
    #endregion

    #region Async Methods (New - Performance Optimized)

    /// <summary>
    /// Get all audit entries asynchronously (paginated recommended)
    /// </summary>
    public async Task<List<AuditEntry>> GetAllAsync() =>
        await _db.AuditEntries
           .Where(a => a.CaseReviewId == null)
           .OrderByDescending(a => a.Timestamp)
           .ToListAsync();

    /// <summary>
    /// Get paginated audit entries
    /// </summary>
    public async Task<PaginatedResult<AuditEntry>> GetAllPaginatedAsync(int pageNumber = 1, int pageSize = 50)
    {
        var pagination = new PaginationParams { PageNumber = pageNumber, PageSize = pageSize };

        var total = await _db.AuditEntries
            .Where(a => a.CaseReviewId == null)
            .CountAsync();

        var entries = await _db.AuditEntries
           .Where(a => a.CaseReviewId == null)
           .OrderByDescending(a => a.Timestamp)
           .Skip(pagination.Skip)
           .Take(pagination.Take)
           .ToListAsync();

        return new PaginatedResult<AuditEntry>
        {
            Items = entries,
            TotalCount = total,
            CurrentPage = pagination.PageNumber,
            PageSize = pagination.PageSize
        };
    }

    /// <summary>
    /// Get audit entries by claim ID asynchronously
    /// </summary>
    public async Task<List<AuditEntry>> GetByClaimIdAsync(string claimId) =>
        await _db.AuditEntries
           .Where(a => a.ClaimId == claimId)
           .OrderByDescending(a => a.Timestamp)
           .ToListAsync();

    /// <summary>
    /// Add audit entry asynchronously
    /// </summary>
    public async Task AddAsync(AuditEntry entry)
    {
        entry.AuditId = $"AUD-{Interlocked.Increment(ref _counter):D5}";
        _db.AuditEntries.Add(entry);
        await _db.SaveChangesAsync();
    }

    #endregion
}
