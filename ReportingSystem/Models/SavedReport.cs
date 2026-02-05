using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReportingSystem.Models;

/// <summary>
/// Represents a saved ad-hoc report configuration that users can create, save, and re-run.
/// </summary>
public class SavedReport
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    [Display(Name = "Report Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    /// <summary>
    /// The type of report (determines which data sources and fields are available)
    /// </summary>
    [Required]
    [StringLength(50)]
    public string ReportType { get; set; } = SavedReportType.Reports;

    /// <summary>
    /// JSON-serialized filter configuration
    /// </summary>
    [Required]
    public string FilterConfiguration { get; set; } = "{}";

    /// <summary>
    /// JSON-serialized list of selected columns to display
    /// </summary>
    public string? SelectedColumns { get; set; }

    /// <summary>
    /// JSON-serialized sort configuration (column, direction)
    /// </summary>
    public string? SortConfiguration { get; set; }

    /// <summary>
    /// JSON-serialized grouping configuration
    /// </summary>
    public string? GroupingConfiguration { get; set; }

    /// <summary>
    /// Whether this report is shared with other users or private
    /// </summary>
    [Display(Name = "Is Public")]
    public bool IsPublic { get; set; }

    /// <summary>
    /// Whether this report appears on the user's dashboard
    /// </summary>
    [Display(Name = "Pin to Dashboard")]
    public bool IsPinnedToDashboard { get; set; }

    /// <summary>
    /// The user who created this saved report
    /// </summary>
    public int CreatedById { get; set; }
    [ForeignKey(nameof(CreatedById))]
    public User CreatedBy { get; set; } = null!;

    [Display(Name = "Created At")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Display(Name = "Last Modified")]
    public DateTime? ModifiedAt { get; set; }

    [Display(Name = "Last Run")]
    public DateTime? LastRunAt { get; set; }

    /// <summary>
    /// Number of times this report has been run
    /// </summary>
    public int RunCount { get; set; }

    /// <summary>
    /// Preferred export format (csv, excel, pdf)
    /// </summary>
    [StringLength(20)]
    public string? DefaultExportFormat { get; set; }
}

/// <summary>
/// Types of saved reports available in the Report Builder
/// </summary>
public static class SavedReportType
{
    public const string Reports = "reports";
    public const string SuggestedActions = "suggested_actions";
    public const string ResourceRequests = "resource_requests";
    public const string SupportRequests = "support_requests";
    public const string AuditLog = "audit_log";
    public const string Aggregation = "aggregation";
    public const string Users = "users";
    public const string Feedback = "feedback";
    public const string Recommendations = "recommendations";

    public static string DisplayName(string type) => type switch
    {
        Reports => "Reports",
        SuggestedActions => "Suggested Actions",
        ResourceRequests => "Resource Requests",
        SupportRequests => "Support Requests",
        AuditLog => "Audit Log",
        Aggregation => "Aggregation Summary",
        Users => "Users",
        Feedback => "Feedback",
        Recommendations => "Recommendations",
        _ => type
    };

    public static IEnumerable<(string Value, string Display)> GetAll()
    {
        yield return (Reports, "Reports");
        yield return (SuggestedActions, "Suggested Actions");
        yield return (ResourceRequests, "Resource Requests");
        yield return (SupportRequests, "Support Requests");
        yield return (Feedback, "Feedback");
        yield return (Recommendations, "Recommendations");
        yield return (AuditLog, "Audit Log");
        yield return (Aggregation, "Aggregation Summary");
        yield return (Users, "Users");
    }
}
