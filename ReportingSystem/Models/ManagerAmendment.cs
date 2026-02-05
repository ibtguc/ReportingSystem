using System.ComponentModel.DataAnnotations;

namespace ReportingSystem.Models;

/// <summary>
/// Allows managers to annotate or amend aggregated data with additional context,
/// corrections, or synthesized summaries. Maintains clear distinction between
/// original aggregated values and manager additions.
/// </summary>
public class ManagerAmendment
{
    public int Id { get; set; }

    [Required]
    public int AggregatedValueId { get; set; }

    [Required]
    public int AmendedById { get; set; }

    [Required]
    [StringLength(30)]
    [Display(Name = "Amendment Type")]
    public string AmendmentType { get; set; } = AmendmentTypes.Annotation;

    [StringLength(200)]
    [Display(Name = "Title")]
    public string? Title { get; set; }

    // The amended/overridden value (for corrections)
    [StringLength(4000)]
    [Display(Name = "Amended Value")]
    public string? AmendedValue { get; set; }

    // Numeric version for sorting/comparison
    [Display(Name = "Amended Numeric Value")]
    public double? AmendedNumericValue { get; set; }

    // Manager's annotation/context
    [Required]
    [StringLength(4000)]
    [Display(Name = "Annotation")]
    public string Annotation { get; set; } = string.Empty;

    // Justification for corrections/overrides
    [StringLength(2000)]
    [Display(Name = "Justification")]
    public string? Justification { get; set; }

    // Supporting data or references (JSON)
    [StringLength(2000)]
    [Display(Name = "Supporting Data (JSON)")]
    public string? SupportingDataJson { get; set; }

    // Executive summary synthesized from subordinate reports
    [StringLength(4000)]
    [Display(Name = "Executive Summary")]
    public string? ExecutiveSummary { get; set; }

    // Key insights extracted from aggregation
    [StringLength(2000)]
    [Display(Name = "Key Insights (JSON)")]
    public string? KeyInsightsJson { get; set; }

    // Visibility of this amendment
    [Required]
    [StringLength(30)]
    [Display(Name = "Visibility")]
    public string Visibility { get; set; } = AmendmentVisibility.SameLevel;

    // Whether this amendment is the currently active one
    [Display(Name = "Is Active")]
    public bool IsActive { get; set; } = true;

    // Approval workflow for amendments
    [StringLength(30)]
    [Display(Name = "Approval Status")]
    public string? ApprovalStatus { get; set; }

    public int? ApprovedById { get; set; }
    public DateTime? ApprovedAt { get; set; }

    [StringLength(500)]
    [Display(Name = "Approval Comments")]
    public string? ApprovalComments { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public AggregatedValue AggregatedValue { get; set; } = null!;
    public User AmendedBy { get; set; } = null!;
    public User? ApprovedBy { get; set; }

    // Computed properties
    public bool IsCorrection => AmendmentType == AmendmentTypes.Correction;
    public bool IsAnnotation => AmendmentType == AmendmentTypes.Annotation;
    public bool IsSummary => AmendmentType == AmendmentTypes.ExecutiveSummary;
    public bool RequiresApproval => IsCorrection && ApprovalStatus == AmendmentApprovalStatus.Pending;
    public bool IsApproved => ApprovalStatus == AmendmentApprovalStatus.Approved;

    public string AmendmentTypeDisplayName => AmendmentTypes.DisplayName(AmendmentType);
    public string AmendmentTypeBadgeClass => AmendmentTypes.BadgeClass(AmendmentType);
    public string VisibilityDisplayName => AmendmentVisibility.DisplayName(Visibility);
}

/// <summary>
/// Types of amendments managers can make.
/// </summary>
public static class AmendmentTypes
{
    public const string Annotation = "Annotation";
    public const string Correction = "Correction";
    public const string ExecutiveSummary = "ExecutiveSummary";
    public const string ContextualNote = "ContextualNote";
    public const string Highlight = "Highlight";
    public const string Warning = "Warning";

    public static string DisplayName(string type) => type switch
    {
        Annotation => "Annotation",
        Correction => "Correction",
        ExecutiveSummary => "Executive Summary",
        ContextualNote => "Contextual Note",
        Highlight => "Highlight",
        Warning => "Warning",
        _ => type
    };

    public static string BadgeClass(string type) => type switch
    {
        Annotation => "bg-info",
        Correction => "bg-warning",
        ExecutiveSummary => "bg-primary",
        ContextualNote => "bg-secondary",
        Highlight => "bg-success",
        Warning => "bg-danger",
        _ => "bg-secondary"
    };

    public static IEnumerable<string> All => new[] { Annotation, Correction, ExecutiveSummary, ContextualNote, Highlight, Warning };
}

/// <summary>
/// Visibility levels for amendments.
/// </summary>
public static class AmendmentVisibility
{
    public const string Private = "Private";
    public const string SameLevel = "SameLevel";
    public const string UpwardOnly = "UpwardOnly";
    public const string DownwardOnly = "DownwardOnly";
    public const string AllLevels = "AllLevels";

    public static string DisplayName(string visibility) => visibility switch
    {
        Private => "Private (Only Me)",
        SameLevel => "Same Level",
        UpwardOnly => "Upward Only",
        DownwardOnly => "Downward Only",
        AllLevels => "All Levels",
        _ => visibility
    };

    public static IEnumerable<string> All => new[] { Private, SameLevel, UpwardOnly, DownwardOnly, AllLevels };
}

/// <summary>
/// Approval status for amendments (especially corrections).
/// </summary>
public static class AmendmentApprovalStatus
{
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";

    public static string DisplayName(string status) => status switch
    {
        Pending => "Pending Approval",
        Approved => "Approved",
        Rejected => "Rejected",
        _ => status
    };

    public static string BadgeClass(string status) => status switch
    {
        Pending => "bg-warning",
        Approved => "bg-success",
        Rejected => "bg-danger",
        _ => "bg-secondary"
    };

    public static IEnumerable<string> All => new[] { Pending, Approved, Rejected };
}
