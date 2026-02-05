using System.ComponentModel.DataAnnotations;

namespace ReportingSystem.Models;

/// <summary>
/// Represents a suggested action/improvement submitted as part of a report.
/// Used for process improvements, innovations, cost reductions, etc.
/// </summary>
public class SuggestedAction
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
    [Display(Name = "Justification")]
    public string? Justification { get; set; }

    [StringLength(1000)]
    [Display(Name = "Expected Outcome")]
    public string? ExpectedOutcome { get; set; }

    [StringLength(500)]
    [Display(Name = "Timeline")]
    public string? Timeline { get; set; }

    [Required]
    [StringLength(50)]
    [Display(Name = "Category")]
    public string Category { get; set; } = ActionCategory.ProcessImprovement;

    [Required]
    [StringLength(20)]
    [Display(Name = "Priority")]
    public string Priority { get; set; } = ActionPriority.Medium;

    [Required]
    [StringLength(30)]
    [Display(Name = "Status")]
    public string Status { get; set; } = ActionStatus.Submitted;

    [StringLength(1000)]
    [Display(Name = "Review Comments")]
    public string? ReviewComments { get; set; }

    [Display(Name = "Reviewed By")]
    public int? ReviewedById { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public Report Report { get; set; } = null!;
    public User? ReviewedBy { get; set; }

    /// <summary>
    /// Display name combining title and status.
    /// </summary>
    public string DisplayName => $"{Title} ({ActionStatus.DisplayName(Status)})";
}

/// <summary>
/// Categories for suggested actions.
/// </summary>
public static class ActionCategory
{
    public const string ProcessImprovement = "ProcessImprovement";
    public const string CostReduction = "CostReduction";
    public const string QualityEnhancement = "QualityEnhancement";
    public const string Innovation = "Innovation";
    public const string RiskMitigation = "RiskMitigation";

    public static readonly string[] All = [ProcessImprovement, CostReduction, QualityEnhancement, Innovation, RiskMitigation];

    public static string DisplayName(string category) => category switch
    {
        ProcessImprovement => "Process Improvement",
        CostReduction => "Cost Reduction",
        QualityEnhancement => "Quality Enhancement",
        Innovation => "Innovation",
        RiskMitigation => "Risk Mitigation",
        _ => category
    };
}

/// <summary>
/// Priority levels for suggested actions.
/// </summary>
public static class ActionPriority
{
    public const string Critical = "Critical";
    public const string High = "High";
    public const string Medium = "Medium";
    public const string Low = "Low";

    public static readonly string[] All = [Critical, High, Medium, Low];

    public static string DisplayName(string priority) => priority;

    public static string BadgeClass(string priority) => priority switch
    {
        Critical => "bg-danger",
        High => "bg-warning text-dark",
        Medium => "bg-info",
        Low => "bg-secondary",
        _ => "bg-secondary"
    };
}

/// <summary>
/// Status values for suggested actions.
/// </summary>
public static class ActionStatus
{
    public const string Submitted = "Submitted";
    public const string UnderReview = "UnderReview";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
    public const string Implemented = "Implemented";
    public const string Deferred = "Deferred";

    public static readonly string[] All = [Submitted, UnderReview, Approved, Rejected, Implemented, Deferred];

    public static string DisplayName(string status) => status switch
    {
        Submitted => "Submitted",
        UnderReview => "Under Review",
        Approved => "Approved",
        Rejected => "Rejected",
        Implemented => "Implemented",
        Deferred => "Deferred",
        _ => status
    };

    public static string BadgeClass(string status) => status switch
    {
        Submitted => "bg-primary",
        UnderReview => "bg-warning text-dark",
        Approved => "bg-success",
        Rejected => "bg-danger",
        Implemented => "bg-info",
        Deferred => "bg-secondary",
        _ => "bg-secondary"
    };
}
