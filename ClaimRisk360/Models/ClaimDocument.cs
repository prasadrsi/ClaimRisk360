using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClaimRisk360.Models;

public class ClaimDocument
{
    [Key]
    public string DocumentId { get; set; } = string.Empty;
    public string ClaimId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public DateTime UploadedAt { get; set; }
    public string UploadedBy { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
    public string Status { get; set; } = "Uploaded";

    /// <summary>Simulated document content (HTML) for viewer.</summary>
    public string Content { get; set; } = string.Empty;

    [NotMapped]
    public string FileSizeDisplay => FileSizeBytes switch
    {
        < 1024 => $"{FileSizeBytes} B",
        < 1048576 => $"{FileSizeBytes / 1024.0:F1} KB",
        _ => $"{FileSizeBytes / 1048576.0:F1} MB"
    };

    [NotMapped]
    public string IconClass => DocumentType switch
    {
        "Medical Report" => "bi-file-earmark-medical text-danger",
        "Invoice" or "Receipt" => "bi-receipt text-success",
        "Lab Result" => "bi-clipboard2-pulse text-info",
        "Prescription" => "bi-capsule text-warning",
        "ID Proof" => "bi-person-badge text-primary",
        "Insurance Card" => "bi-credit-card text-secondary",
        "Referral Letter" => "bi-envelope-paper text-primary",
        _ => "bi-file-earmark text-muted"
    };

    [NotMapped]
    public string StatusBadgeClass => Status switch
    {
        "Uploaded" => "bg-success",
        "Verified" => "bg-primary",
        "Rejected" => "bg-danger",
        "Processing" => "bg-warning text-dark",
        _ => "bg-secondary"
    };
}
