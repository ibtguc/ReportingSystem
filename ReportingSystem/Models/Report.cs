using System.ComponentModel.DataAnnotations;

namespace ReportingSystem.Models;

/// <summary>
/// A report instance: a specific user's submission for a specific period and template.
/// Tracks the full lifecycle from Draft through Approved/Rejected.
/// </summary>
public class Report
{
    public int Id { get; set; }

    [Required]
    public int ReportTemplateId { get; set; }

    [Required]
    public int ReportPeriodId { get; set; }

    [Required]
    [Display(Name = "Submitted By")]
    public int SubmittedById { get; set; }

    [Display(Name = "Assigned To (Reviewer)")]
    public int? AssignedReviewerId { get; set; }

    [Required]
    [StringLength(50)]
    [Display(Name = "Status")]
    public string Status { get; set; } = ReportStatus.Draft;

    [DataType(DataType.DateTime)]
    [Display(Name = "Submitted At")]
    public DateTime? SubmittedAt { get; set; }

    [DataType(DataType.DateTime)]
    [Display(Name = "Reviewed At")]
    public DateTime? ReviewedAt { get; set; }

    [StringLength(2000)]
    [Display(Name = "Review Comments")]
    public string? ReviewComments { get; set; }

    [Display(Name = "Locked")]
    public bool IsLocked { get; set; }

    [DataType(DataType.DateTime)]
    [Display(Name = "Last Auto-Save")]
    public DateTime? LastAutoSaveAt { get; set; }

    [Display(Name = "Amendment Count")]
    public int AmendmentCount { get; set; }

    [Display(Name = "Pre-Populated")]
    public bool WasPrePopulated { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public ReportTemplate ReportTemplate { get; set; } = null!;
    public ReportPeriod ReportPeriod { get; set; } = null!;
    public User SubmittedBy { get; set; } = null!;
    public User? AssignedReviewer { get; set; }
    public ICollection<ReportFieldValue> FieldValues { get; set; } = new List<ReportFieldValue>();
    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();

    /// <summary>
    /// Whether this report can still be edited.
    /// </summary>
    public bool IsEditable =>
        !IsLocked && (Status == ReportStatus.Draft || Status == ReportStatus.Amended);

    /// <summary>
    /// Display-friendly status.
    /// </summary>
    public string StatusDisplayName => ReportStatus.DisplayName(Status);
}

/// <summary>
/// Report lifecycle statuses per the SRS.
/// </summary>
public static class ReportStatus
{
    public const string Draft = "Draft";
    public const string Submitted = "Submitted";
    public const string UnderReview = "UnderReview";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
    public const string Amended = "Amended";

    public static readonly string[] All =
    [
        Draft, Submitted, UnderReview, Approved, Rejected, Amended
    ];

    public static string DisplayName(string status) => status switch
    {
        Draft => "Draft",
        Submitted => "Submitted",
        UnderReview => "Under Review",
        Approved => "Approved",
        Rejected => "Rejected",
        Amended => "Amended",
        _ => status
    };

    public static string BadgeClass(string status) => status switch
    {
        Draft => "bg-secondary",
        Submitted => "bg-info",
        UnderReview => "bg-warning text-dark",
        Approved => "bg-success",
        Rejected => "bg-danger",
        Amended => "bg-primary",
        _ => "bg-secondary"
    };
}
