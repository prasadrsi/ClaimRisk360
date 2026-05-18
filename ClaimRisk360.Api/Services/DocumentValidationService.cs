using ClaimRisk360.Api.Models;

namespace ClaimRisk360.Api.Services;

/// <summary>
/// Service that validates claim documents for completeness and integrity.
/// </summary>
public class DocumentValidationService
{
    private static readonly HashSet<string> AllowedDocumentTypes =
    [
        "Medical Record", "Invoice", "Referral", "Lab Report",
        "Prescription", "Discharge Summary", "Authorization"
    ];

    private static readonly HashSet<string> AllowedExtensions =
    [
        ".pdf", ".png", ".jpg", ".jpeg", ".tiff", ".docx"
    ];

    private const long MaxFileSizeBytes = 25 * 1024 * 1024; // 25 MB

    public DocumentValidationResponse Validate(DocumentValidationRequest request)
    {
        var response = new DocumentValidationResponse
        {
            DocumentId = request.DocumentId,
            ClaimId = request.ClaimId
        };

        // Validate document type
        if (!AllowedDocumentTypes.Contains(request.DocumentType))
        {
            response.Issues.Add(new DocumentIssue
            {
                Code = "INVALID_DOC_TYPE",
                Severity = "Error",
                Message = $"Document type '{request.DocumentType}' is not accepted"
            });
        }

        // Validate file extension
        var extension = Path.GetExtension(request.FileName)?.ToLowerInvariant() ?? string.Empty;
        if (!AllowedExtensions.Contains(extension))
        {
            response.Issues.Add(new DocumentIssue
            {
                Code = "INVALID_EXTENSION",
                Severity = "Error",
                Message = $"File extension '{extension}' is not supported"
            });
        }

        // Validate content is present
        if (string.IsNullOrWhiteSpace(request.ContentBase64))
        {
            response.Issues.Add(new DocumentIssue
            {
                Code = "EMPTY_CONTENT",
                Severity = "Error",
                Message = "Document content is empty"
            });
        }
        else
        {
            // Validate file size
            try
            {
                var bytes = Convert.FromBase64String(request.ContentBase64);
                if (bytes.Length > MaxFileSizeBytes)
                {
                    response.Issues.Add(new DocumentIssue
                    {
                        Code = "FILE_TOO_LARGE",
                        Severity = "Error",
                        Message = $"File size exceeds maximum allowed size of 25 MB"
                    });
                }
            }
            catch (FormatException)
            {
                response.Issues.Add(new DocumentIssue
                {
                    Code = "INVALID_BASE64",
                    Severity = "Error",
                    Message = "Content is not valid Base64 encoded data"
                });
            }
        }

        // Validate claim ID present
        if (string.IsNullOrWhiteSpace(request.ClaimId))
        {
            response.Issues.Add(new DocumentIssue
            {
                Code = "MISSING_CLAIM_ID",
                Severity = "Error",
                Message = "Claim ID is required for document association"
            });
        }

        response.IsValid = response.Issues.Count == 0;
        response.Status = response.IsValid ? "Valid" : "Invalid";

        return response;
    }
}
