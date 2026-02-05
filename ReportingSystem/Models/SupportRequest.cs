using System.ComponentModel.DataAnnotations;

namespace ReportingSystem.Models;

/// <summary>
/// Represents a support request submitted as part of a report.
/// Used for requesting management intervention, coordination, technical assistance, etc.
/// </summary>
public class SupportRequest
{
    public int Id { get; set; }

    [Required]
    public int ReportId { get; set; }

    [Required]
    [StringLength(200)]
    [Display(Name = "Title")]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(2000)]
    [Display(Name = "Description")]
    public string Description { get; set; } = string.Empty;

    [StringLength(1000)]
    [Display(Name = "Current Situation")]
    public string? CurrentSituation { get; set; }

    [StringLength(1000)]
    [Display(Name = "Desired Outcome")]
    public string? DesiredOutcome { get; set; }

    [Required]
    [StringLength(50)]
    [Display(Name = "Category")]
    public string Category { get; set; } = SupportCategory.TechnicalAssistance;

    [Required]
    [StringLength(20)]
    [Display(Name = "Urgency")]
    public string Urgency { get; set; } = SupportUrgency.Medium;

    [Required]
    [StringLength(30)]
    [Display(Name = "Status")]
    public string Status { get; set; } = SupportStatus.Submitted;

    [StringLength(1000)]
    [Display(Name = "Response/Resolution")]
    public string? Resolution { get; set; }

    [Display(Name = "Assigned To")]
    public int? AssignedToId { get; set; }

    [Display(Name = "Acknowledged By")]
    public int? AcknowledgedById { get; set; }

    public DateTime? AcknowledgedAt { get; set; }

    [Display(Name = "Resolved By")]
    public int? ResolvedById { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public Report Report { get; set; } = null!;
    public User? AssignedTo { get; set; }
    public User? AcknowledgedBy { get; set; }
    public User? ResolvedBy { get; set; }

    /// <summary>
    /// Display name combining title and status.
    /// </summary>
    public string DisplayName => $"{Title} ({SupportStatus.DisplayName(Status)})";

    /// <summary>
    /// Whether the request is still open (not resolved or closed).
    /// </summary>
    public bool IsOpen => Status != SupportStatus.Resolved && Status != SupportStatus.Closed;
}

/// <summary>
/// Categories for support requests.
/// </summary>
public static class SupportCategory
{
    public const string ManagementIntervention = "ManagementIntervention";
    public const string CrossDeptCoordination = "CrossDeptCoordination";
    public const string TechnicalAssistance = "TechnicalAssistance";
    public const string Training = "Training";
    public const string ConflictResolution = "ConflictResolution";
    public const string PolicyClarification = "PolicyClarification";

    public static readonly string[] All = [ManagementIntervention, CrossDeptCoordination, TechnicalAssistance, Training, ConflictResolution, PolicyClarification];

    public static string DisplayName(string category) => category switch
    {
        ManagementIntervention => "Management Intervention",
        CrossDeptCoordination => "Cross-Dept Coordination",
        TechnicalAssistance => "Technical Assistance",
        Training => "Training",
        ConflictResolution => "Conflict Resolution",
        PolicyClarification => "Policy Clarification",
        _ => category
    };
}

/// <summary>
/// Urgency levels for support requests.
/// </summary>
public static class SupportUrgency
{
    public const string Critical = "Critical";
    public const string High = "High";
    public const string Medium = "Medium";
    public const string Low = "Low";

    public static readonly string[] All = [Critical, High, Medium, Low];

    public static string DisplayName(string urgency) => urgency;

    public static string BadgeClass(string urgency) => urgency switch
    {
        Critical => "bg-danger",
        High => "bg-warning text-dark",
        Medium => "bg-info",
        Low => "bg-secondary",
        _ => "bg-secondary"
    };
}

/// <summary>
/// Status values for support requests.
/// </summary>
public static class SupportStatus
{
    public const string Submitted = "Submitted";
    public const string Acknowledged = "Acknowledged";
    public const string InProgress = "InProgress";
    public const string Resolved = "Resolved";
    public const string Closed = "Closed";

    public static readonly string[] All = [Submitted, Acknowledged, InProgress, Resolved, Closed];

    public static string DisplayName(string status) => status switch
    {
        Submitted => "Submitted",
        Acknowledged => "Acknowledged",
        InProgress => "In Progress",
        Resolved => "Resolved",
        Closed => "Closed",
        _ => status
    };

    public static string BadgeClass(string status) => status switch
    {
        Submitted => "bg-primary",
        Acknowledged => "bg-info",
        InProgress => "bg-warning text-dark",
        Resolved => "bg-success",
        Closed => "bg-dark",
        _ => "bg-secondary"
    };
}
