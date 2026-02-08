using System.ComponentModel.DataAnnotations;

namespace ReportingSystem.Models;

public class Attachment
{
    public int Id { get; set; }

    public int ReportId { get; set; }

    [Required]
    [StringLength(255)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string StoragePath { get; set; } = string.Empty;

    [StringLength(100)]
    public string? ContentType { get; set; }

    public long FileSizeBytes { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public int UploadedById { get; set; }

    // Navigation properties
    public Report Report { get; set; } = null!;
    public User UploadedBy { get; set; } = null!;
}
