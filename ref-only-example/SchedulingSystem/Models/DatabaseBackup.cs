using System.ComponentModel.DataAnnotations;

namespace SchedulingSystem.Models;

/// <summary>
/// Represents a database backup record
/// </summary>
public class DatabaseBackup
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    [Display(Name = "Backup Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Required]
    [StringLength(500)]
    [Display(Name = "File Name")]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [StringLength(1000)]
    [Display(Name = "File Path")]
    public string FilePath { get; set; } = string.Empty;

    [Display(Name = "File Size (bytes)")]
    public long FileSizeBytes { get; set; }

    [Required]
    [Display(Name = "Created At")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [Display(Name = "Backup Type")]
    public BackupType Type { get; set; } = BackupType.Manual;

    [Display(Name = "Created By")]
    public string? CreatedBy { get; set; }

    [Display(Name = "Is Automatic Daily Backup")]
    public bool IsAutomaticDailyBackup { get; set; }
}

/// <summary>
/// Types of database backups
/// </summary>
public enum BackupType
{
    [Display(Name = "Manual")]
    Manual,

    [Display(Name = "Automatic Daily")]
    AutomaticDaily,

    [Display(Name = "Pre-Restore")]
    PreRestore
}
