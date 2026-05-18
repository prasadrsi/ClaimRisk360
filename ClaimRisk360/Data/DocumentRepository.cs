using ClaimRisk360.Models;
using Microsoft.EntityFrameworkCore;

namespace ClaimRisk360.Data;

/// <summary>
/// Data layer: loads and stores claim documents from SQLite via EF Core.
/// Includes async methods for performance optimization.
/// </summary>
public class DocumentRepository
{
    private readonly AppDbContext _db;
    private int _counter;

    public DocumentRepository(AppDbContext db)
    {
        _db = db;
        _counter = _db.ClaimDocuments.Count();
    }

    #region Synchronous Methods (Legacy)
    public List<ClaimDocument> GetByClaimId(string claimId) =>
        _db.ClaimDocuments
           .Where(d => d.ClaimId == claimId)
           .OrderByDescending(d => d.UploadedAt)
           .ToList();

    public ClaimDocument? GetDocument(string documentId) =>
        _db.ClaimDocuments.FirstOrDefault(d => d.DocumentId == documentId);

    public void Add(ClaimDocument document)
    {
        document.DocumentId = $"DOC-{Interlocked.Increment(ref _counter):D5}";
        document.Version = _db.ClaimDocuments.Count(d => d.ClaimId == document.ClaimId && d.FileName == document.FileName) + 1;
        _db.ClaimDocuments.Add(document);
        _db.SaveChanges();
    }
    #endregion

    #region Async Methods (New - Performance Optimized)

    /// <summary>
    /// Get documents by claim ID asynchronously
    /// </summary>
    public async Task<List<ClaimDocument>> GetByClaimIdAsync(string claimId) =>
        await _db.ClaimDocuments
           .Where(d => d.ClaimId == claimId)
           .OrderByDescending(d => d.UploadedAt)
           .ToListAsync();

    /// <summary>
    /// Get single document asynchronously
    /// </summary>
    public async Task<ClaimDocument?> GetDocumentAsync(string documentId) =>
        await _db.ClaimDocuments.FirstOrDefaultAsync(d => d.DocumentId == documentId);

    /// <summary>
    /// Add document asynchronously
    /// </summary>
    public async Task AddAsync(ClaimDocument document)
    {
        document.DocumentId = $"DOC-{Interlocked.Increment(ref _counter):D5}";
        document.Version = await _db.ClaimDocuments.CountAsync(d => d.ClaimId == document.ClaimId && d.FileName == document.FileName) + 1;
        _db.ClaimDocuments.Add(document);
        await _db.SaveChangesAsync();
    }

    #endregion
}
