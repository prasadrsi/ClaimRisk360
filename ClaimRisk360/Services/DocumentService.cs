using ClaimRisk360.Data;
using ClaimRisk360.Models;

namespace ClaimRisk360.Services;

/// <summary>
/// Business Logic: document management operations.
/// Data persistence is handled by DocumentRepository (Data Layer).
/// </summary>
public class DocumentService
{
    private readonly DocumentRepository _repo;
    private readonly AuditService _auditService;

    public DocumentService(DocumentRepository repo, AuditService auditService)
    {
        _repo = repo;
        _auditService = auditService;
    }

    public List<ClaimDocument> GetByClaimId(string claimId) => _repo.GetByClaimId(claimId);

    public ClaimDocument? GetDocument(string documentId) => _repo.GetDocument(documentId);

    public void AddDocument(string claimId, string fileName, string documentType, long fileSize, string uploadedBy)
    {
        var doc = new ClaimDocument
        {
            ClaimId = claimId,
            FileName = fileName,
            DocumentType = documentType,
            FileSizeBytes = fileSize,
            UploadedAt = DateTime.UtcNow,
            UploadedBy = uploadedBy,
            Status = "Uploaded"
        };

        _repo.Add(doc);
        _auditService.Log(claimId, "Document Uploaded", uploadedBy,
            $"{documentType}: {fileName} ({doc.FileSizeDisplay})", "Ingestion");
    }
}
