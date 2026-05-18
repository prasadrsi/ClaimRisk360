using ClaimRisk360.Models;
using Microsoft.EntityFrameworkCore;

namespace ClaimRisk360.Data;

/// <summary>
/// Data layer: loads and stores claim data from SQLite via EF Core.
/// Includes async methods and pagination for performance optimization.
/// </summary>
public class ClaimRepository
{
    private readonly AppDbContext _db;

    public ClaimRepository(AppDbContext db) => _db = db;

    #region Synchronous Methods (Legacy - for compatibility)
    public List<Claim> GetAllClaims() =>
        _db.Claims.OrderByDescending(c => c.FraudRiskScore).ToList();

    public Claim? GetClaim(string claimId) =>
        _db.Claims.FirstOrDefault(c => c.ClaimId == claimId);

    public void AddClaim(Claim claim)
    {
        _db.Claims.Add(claim);
        _db.SaveChanges();
    }

    public void SaveChanges() => _db.SaveChanges();

    public List<FraudRing> GetAllFraudRings() =>
        _db.FraudRings
           .AsSplitQuery()  // Split into separate queries for performance
           .Include(r => r.Nodes)
           .Include(r => r.Edges)
           .ToList();

    public FraudRing? GetFraudRing(string ringId) =>
        _db.FraudRings
           .AsSplitQuery()
           .Include(r => r.Nodes)
           .Include(r => r.Edges)
           .FirstOrDefault(r => r.RingId == ringId);
    #endregion

    #region Async Methods (New - Performance Optimized)

    /// <summary>
    /// Get paginated claims ordered by fraud risk score
    /// </summary>
    public async Task<PaginatedResult<Claim>> GetClaimsPaginatedAsync(int pageNumber = 1, int pageSize = 50)
    {
        var pagination = new PaginationParams { PageNumber = pageNumber, PageSize = pageSize };

        var total = await _db.Claims.CountAsync();
        var claims = await _db.Claims
            .OrderByDescending(c => c.FraudRiskScore)
            .Skip(pagination.Skip)
            .Take(pagination.Take)
            .ToListAsync();

        return new PaginatedResult<Claim>
        {
            Items = claims,
            TotalCount = total,
            CurrentPage = pagination.PageNumber,
            PageSize = pagination.PageSize
        };
    }

    /// <summary>
    /// Get all claims asynchronously (use pagination for large datasets)
    /// </summary>
    public async Task<List<Claim>> GetAllClaimsAsync() =>
        await _db.Claims.OrderByDescending(c => c.FraudRiskScore).ToListAsync();

    /// <summary>
    /// Get single claim asynchronously
    /// </summary>
    public async Task<Claim?> GetClaimAsync(string claimId) =>
        await _db.Claims.FirstOrDefaultAsync(c => c.ClaimId == claimId);

    /// <summary>
    /// Get duplicate claims for a given claim (SQL-based - much faster than in-memory)
    /// </summary>
    public async Task<List<Claim>> GetDuplicatesAsync(Claim claim)
    {
        var minDate = claim.SubmissionDate.AddDays(-3);
        var maxDate = claim.SubmissionDate.AddDays(3);

        return await _db.Claims
            .Where(c => c.ClaimId != claim.ClaimId &&
                        c.PatientId == claim.PatientId &&
                        c.ProviderId == claim.ProviderId &&
                        c.DiagnosisCode == claim.DiagnosisCode &&
                        c.SubmissionDate >= minDate &&
                        c.SubmissionDate <= maxDate)
            .ToListAsync();
    }

    /// <summary>
    /// Add claim asynchronously
    /// </summary>
    public async Task AddClaimAsync(Claim claim)
    {
        _db.Claims.Add(claim);
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Save changes asynchronously
    /// </summary>
    public async Task SaveChangesAsync() => 
        await _db.SaveChangesAsync();

    /// <summary>
    /// Get all fraud rings asynchronously (optimized with AsSplitQuery)
    /// </summary>
    public async Task<List<FraudRing>> GetAllFraudRingsAsync() =>
        await _db.FraudRings
           .AsSplitQuery()  // Split into separate queries to avoid Cartesian explosion
           .Include(r => r.Nodes)
           .Include(r => r.Edges)
           .ToListAsync();

    /// <summary>
    /// Get fraud ring by ID asynchronously
    /// </summary>
    public async Task<FraudRing?> GetFraudRingAsync(string ringId) =>
        await _db.FraudRings
           .AsSplitQuery()
           .Include(r => r.Nodes)
           .Include(r => r.Edges)
           .FirstOrDefaultAsync(r => r.RingId == ringId);

    /// <summary>
    /// Get fraud ring summaries (lightweight - for list views)
    /// </summary>
    public async Task<List<(string RingId, string Label, int NodeCount, int EdgeCount)>> GetFraudRingSummariesAsync()
    {
        return await _db.FraudRings
            .Select(r => new ValueTuple<string, string, int, int>(
                r.RingId,
                r.Label,
                r.Nodes.Count,
                r.Edges.Count))
            .ToListAsync();
    }

    /// <summary>
    /// Get claims by patient (for pattern analysis)
    /// </summary>
    public async Task<List<Claim>> GetClaimsByPatientAsync(string patientId) =>
        await _db.Claims
            .Where(c => c.PatientId == patientId)
            .OrderByDescending(c => c.SubmissionDate)
            .ToListAsync();

    /// <summary>
    /// Get claims by provider (for pattern analysis)
    /// </summary>
    public async Task<List<Claim>> GetClaimsByProviderAsync(string providerId) =>
        await _db.Claims
            .Where(c => c.ProviderId == providerId)
            .OrderByDescending(c => c.SubmissionDate)
            .ToListAsync();

    #endregion
}
