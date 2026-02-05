using System.ComponentModel.DataAnnotations;

namespace ReportingSystem.Models;

/// <summary>
/// File attachment associated with a report.
/// Supports configurable size limits and allowed file types per template.
/// </summary>
public class Attachment
{
    public int Id { get; set; }

    [Required]
    public int ReportId { get; set; }

    /// <summary>
    /// Optional link to a specific field (for FileUpload field types).
    /// </summary>
    public int? ReportFieldId { get; set; }

    [Required]
    [StringLength(255)]
    [Display(Name = "File Name")]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    [Display(Name = "Original File Name")]
    public string OriginalFileName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    [Display(Name = "Content Type")]
    public string ContentType { get; set; } = string.Empty;

    [Display(Name = "File Size (bytes)")]
    public long FileSizeBytes { get; set; }

    [Required]
    [StringLength(1000)]
    [Display(Name = "Storage Path")]
    public string StoragePath { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Display(Name = "Uploaded By")]
    public int UploadedById { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Report Report { get; set; } = null!;
    public ReportField? ReportField { get; set; }
    public User UploadedBy { get; set; } = null!;

    /// <summary>
    /// Human-readable file size.
    /// </summary>
    public string FileSizeDisplay
    {
        get
        {
            if (FileSizeBytes < 1024) return $"{FileSizeBytes} B";
            if (FileSizeBytes < 1024 * 1024) return $"{FileSizeBytes / 1024.0:F1} KB";
            return $"{FileSizeBytes / (1024.0 * 1024.0):F1} MB";
        }
    }
}
