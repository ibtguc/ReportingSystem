using System.ComponentModel.DataAnnotations;

namespace ReportingSystem.Models;

/// <summary>
/// Defines a specific reporting period for a template.
/// E.g., "January 2026" for a monthly template, or "Q1 2026" for quarterly.
/// </summary>
public class ReportPeriod
{
    public int Id { get; set; }

    [Required]
    public int ReportTemplateId { get; set; }

    [Required]
    [StringLength(100)]
    [Display(Name = "Period Name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Start Date")]
    public DateTime StartDate { get; set; }

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "End Date")]
    public DateTime EndDate { get; set; }

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Submission Deadline")]
    public DateTime SubmissionDeadline { get; set; }

    [Display(Name = "Grace Period (days)")]
    [Range(0, 30)]
    public int GracePeriodDays { get; set; } = 3;

    [Display(Name = "Status")]
    public PeriodStatus Status { get; set; } = PeriodStatus.Upcoming;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ReportTemplate ReportTemplate { get; set; } = null!;
    public ICollection<Report> Reports { get; set; } = new List<Report>();

    /// <summary>
    /// Whether the period is currently accepting submissions.
    /// </summary>
    public bool IsOpen =>
        Status == PeriodStatus.Open &&
        DateTime.UtcNow <= SubmissionDeadline.AddDays(GracePeriodDays);

    /// <summary>
    /// Whether the submission deadline has passed.
    /// </summary>
    public bool IsOverdue =>
        DateTime.UtcNow > SubmissionDeadline &&
        DateTime.UtcNow <= SubmissionDeadline.AddDays(GracePeriodDays);

    /// <summary>
    /// Whether the grace period has also expired.
    /// </summary>
    public bool IsFullyClosed =>
        DateTime.UtcNow > SubmissionDeadline.AddDays(GracePeriodDays);

    /// <summary>
    /// Display name including date range.
    /// </summary>
    public string DisplayName =>
        $"{Name} ({StartDate:MMM dd} - {EndDate:MMM dd, yyyy})";
}

/// <summary>
/// Status of a reporting period.
/// </summary>
public enum PeriodStatus
{
    [Display(Name = "Upcoming")]
    Upcoming = 0,

    [Display(Name = "Open")]
    Open = 1,

    [Display(Name = "Closed")]
    Closed = 2,

    [Display(Name = "Archived")]
    Archived = 3
}
