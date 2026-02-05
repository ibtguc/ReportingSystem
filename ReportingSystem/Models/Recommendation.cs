using System.ComponentModel.DataAnnotations;

namespace ReportingSystem.Models;

/// <summary>
/// Represents a recommendation/directive from management flowing downward.
/// Used for guidance, directives, and strategic alignment across the hierarchy.
/// </summary>
public class Recommendation
{
    public int Id { get; set; }

    /// <summary>
    /// Optional link to a specific report that triggered this recommendation.
    /// </summary>
    public int? ReportId { get; set; }

    /// <summary>
    /// User issuing the recommendation.
    /// </summary>
    [Required]
    public int IssuedById { get; set; }

    /// <summary>
    /// Target organizational unit for the recommendation.
    /// </summary>
    public int? TargetOrgUnitId { get; set; }

    /// <summary>
    /// Target user for individual recommendations.
    /// </summary>
    public int? TargetUserId { get; set; }

    [Required]
    [StringLength(200)]
    [Display(Name = "Title")]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(4000)]
    [Display(Name = "Description")]
    public string Description { get; set; } = string.Empty;

    [StringLength(2000)]
    [Display(Name = "Rationale")]
    public string? Rationale { get; set; }

    [StringLength(500)]
    [Display(Name = "Timeline")]
    public string? Timeline { get; set; }

    [Required]
    [StringLength(50)]
    [Display(Name = "Category")]
    public string Category { get; set; } = RecommendationCategory.General;

    [Required]
    [StringLength(20)]
    [Display(Name = "Priority")]
    public string Priority { get; set; } = RecommendationPriority.Medium;

    [Required]
    [StringLength(30)]
    [Display(Name = "Target Scope")]
    public string TargetScope { get; set; } = RecommendationScope.Individual;

    [Required]
    [StringLength(30)]
    [Display(Name = "Status")]
    public string Status { get; set; } = RecommendationStatus.Issued;

    /// <summary>
    /// Date when the recommendation becomes effective.
    /// </summary>
    public DateTime? EffectiveDate { get; set; }

    /// <summary>
    /// Date by which the recommendation should be implemented.
    /// </summary>
    public DateTime? DueDate { get; set; }

    /// <summary>
    /// Whether sub-units should also receive this recommendation.
    /// </summary>
    public bool CascadeToSubUnits { get; set; } = false;

    /// <summary>
    /// Number of acknowledgments received (for cascaded recommendations).
    /// </summary>
    public int AcknowledgmentCount { get; set; } = 0;

    /// <summary>
    /// Implementation notes or progress updates.
    /// </summary>
    [StringLength(2000)]
    [Display(Name = "Implementation Notes")]
    public string? ImplementationNotes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    // Navigation properties
    public Report? Report { get; set; }
    public User IssuedBy { get; set; } = null!;
    public OrganizationalUnit? TargetOrgUnit { get; set; }
    public User? TargetUser { get; set; }

    /// <summary>
    /// Display name combining title and status.
    /// </summary>
    public string DisplayName => $"{Title} ({RecommendationStatus.DisplayName(Status)})";

    /// <summary>
    /// Check if recommendation is overdue.
    /// </summary>
    public bool IsOverdue => DueDate.HasValue &&
                             DueDate.Value < DateTime.UtcNow &&
                             Status != RecommendationStatus.Completed &&
                             Status != RecommendationStatus.Cancelled;

    /// <summary>
    /// Days until due date (negative if overdue).
    /// </summary>
    public int? DaysUntilDue => DueDate.HasValue ? (DueDate.Value - DateTime.UtcNow).Days : null;
}

/// <summary>
/// Categories for recommendations.
/// </summary>
public static class RecommendationCategory
{
    public const string ProcessChange = "ProcessChange";
    public const string SkillDevelopment = "SkillDevelopment";
    public const string PerformanceImprovement = "PerformanceImprovement";
    public const string Compliance = "Compliance";
    public const string StrategicAlignment = "StrategicAlignment";
    public const string ResourceOptimization = "ResourceOptimization";
    public const string General = "General";

    public static readonly string[] All = [ProcessChange, SkillDevelopment, PerformanceImprovement,
                                           Compliance, StrategicAlignment, ResourceOptimization, General];

    public static string DisplayName(string category) => category switch
    {
        ProcessChange => "Process Change",
        SkillDevelopment => "Skill Development",
        PerformanceImprovement => "Performance Improvement",
        Compliance => "Compliance",
        StrategicAlignment => "Strategic Alignment",
        ResourceOptimization => "Resource Optimization",
        General => "General",
        _ => category
    };

    public static string BadgeClass(string category) => category switch
    {
        ProcessChange => "bg-info",
        SkillDevelopment => "bg-success",
        PerformanceImprovement => "bg-warning text-dark",
        Compliance => "bg-danger",
        StrategicAlignment => "bg-primary",
        ResourceOptimization => "bg-secondary",
        General => "bg-secondary",
        _ => "bg-secondary"
    };
}

/// <summary>
/// Priority levels for recommendations.
/// </summary>
public static class RecommendationPriority
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
/// Target scope for recommendations.
/// </summary>
public static class RecommendationScope
{
    public const string Individual = "Individual";
    public const string Team = "Team";
    public const string Department = "Department";
    public const string OrganizationWide = "OrganizationWide";

    public static readonly string[] All = [Individual, Team, Department, OrganizationWide];

    public static string DisplayName(string scope) => scope switch
    {
        Individual => "Individual",
        Team => "Team",
        Department => "Department",
        OrganizationWide => "Organization-wide",
        _ => scope
    };

    public static string BadgeClass(string scope) => scope switch
    {
        Individual => "bg-secondary",
        Team => "bg-info",
        Department => "bg-primary",
        OrganizationWide => "bg-success",
        _ => "bg-secondary"
    };
}

/// <summary>
/// Status values for recommendations.
/// </summary>
public static class RecommendationStatus
{
    public const string Draft = "Draft";
    public const string Issued = "Issued";
    public const string Acknowledged = "Acknowledged";
    public const string InProgress = "InProgress";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";

    public static readonly string[] All = [Draft, Issued, Acknowledged, InProgress, Completed, Cancelled];

    public static string DisplayName(string status) => status switch
    {
        Draft => "Draft",
        Issued => "Issued",
        Acknowledged => "Acknowledged",
        InProgress => "In Progress",
        Completed => "Completed",
        Cancelled => "Cancelled",
        _ => status
    };

    public static string BadgeClass(string status) => status switch
    {
        Draft => "bg-secondary",
        Issued => "bg-primary",
        Acknowledged => "bg-info",
        InProgress => "bg-warning text-dark",
        Completed => "bg-success",
        Cancelled => "bg-danger",
        _ => "bg-secondary"
    };
}
