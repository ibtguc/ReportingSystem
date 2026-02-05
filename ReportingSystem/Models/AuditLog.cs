using System.ComponentModel.DataAnnotations;

namespace ReportingSystem.Models;

/// <summary>
/// Tracks all data changes in the system for compliance and data lineage.
/// Records the user, timestamp, entity affected, and before/after values.
/// </summary>
public class AuditLog
{
    public int Id { get; set; }

    // User who made the change (null for system actions)
    public int? UserId { get; set; }

    [StringLength(100)]
    [Display(Name = "User Name")]
    public string? UserName { get; set; }

    [StringLength(200)]
    [Display(Name = "User Email")]
    public string? UserEmail { get; set; }

    [Required]
    [StringLength(30)]
    [Display(Name = "Action")]
    public string Action { get; set; } = AuditAction.Update;

    [Required]
    [StringLength(100)]
    [Display(Name = "Entity Type")]
    public string EntityType { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Entity ID")]
    public int EntityId { get; set; }

    [StringLength(200)]
    [Display(Name = "Entity Name")]
    public string? EntityName { get; set; }

    // Specific field that was changed (optional)
    [StringLength(100)]
    [Display(Name = "Field Name")]
    public string? FieldName { get; set; }

    // Values before and after change (stored as JSON for complex objects)
    [StringLength(4000)]
    [Display(Name = "Old Value")]
    public string? OldValue { get; set; }

    [StringLength(4000)]
    [Display(Name = "New Value")]
    public string? NewValue { get; set; }

    // Full entity snapshot (JSON) for complex changes
    [Display(Name = "Old Entity (JSON)")]
    public string? OldEntityJson { get; set; }

    [Display(Name = "New Entity (JSON)")]
    public string? NewEntityJson { get; set; }

    // Context for the change
    [StringLength(500)]
    [Display(Name = "Reason")]
    public string? Reason { get; set; }

    [StringLength(200)]
    [Display(Name = "IP Address")]
    public string? IpAddress { get; set; }

    [StringLength(500)]
    [Display(Name = "User Agent")]
    public string? UserAgent { get; set; }

    // Related entities for drill-down
    public int? ReportId { get; set; }
    public int? OrganizationalUnitId { get; set; }

    // Correlation ID for grouping related changes
    [StringLength(50)]
    [Display(Name = "Correlation ID")]
    public string? CorrelationId { get; set; }

    [Required]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User? User { get; set; }
    public Report? Report { get; set; }
    public OrganizationalUnit? OrganizationalUnit { get; set; }

    // Computed properties
    public bool IsCreate => Action == AuditAction.Create;
    public bool IsUpdate => Action == AuditAction.Update;
    public bool IsDelete => Action == AuditAction.Delete;
    public bool IsSystemAction => !UserId.HasValue;

    public string ActionDisplayName => AuditAction.DisplayName(Action);
    public string ActionBadgeClass => AuditAction.BadgeClass(Action);

    /// <summary>
    /// Summary of the change for display.
    /// </summary>
    public string ChangeSummary
    {
        get
        {
            if (Action == AuditAction.Create)
                return $"Created {EntityType} '{EntityName ?? EntityId.ToString()}'";
            if (Action == AuditAction.Delete)
                return $"Deleted {EntityType} '{EntityName ?? EntityId.ToString()}'";
            if (!string.IsNullOrEmpty(FieldName))
                return $"Changed {FieldName} from '{TruncateValue(OldValue)}' to '{TruncateValue(NewValue)}'";
            return $"Updated {EntityType} '{EntityName ?? EntityId.ToString()}'";
        }
    }

    private static string TruncateValue(string? value, int maxLength = 50)
    {
        if (string.IsNullOrEmpty(value)) return "(empty)";
        return value.Length <= maxLength ? value : value[..maxLength] + "...";
    }
}

/// <summary>
/// Types of audit actions tracked.
/// </summary>
public static class AuditAction
{
    public const string Create = "Create";
    public const string Update = "Update";
    public const string Delete = "Delete";
    public const string View = "View";
    public const string Export = "Export";
    public const string Login = "Login";
    public const string Logout = "Logout";
    public const string Submit = "Submit";
    public const string Approve = "Approve";
    public const string Reject = "Reject";
    public const string Aggregate = "Aggregate";
    public const string Amend = "Amend";

    public static string DisplayName(string action) => action switch
    {
        Create => "Create",
        Update => "Update",
        Delete => "Delete",
        View => "View",
        Export => "Export",
        Login => "Login",
        Logout => "Logout",
        Submit => "Submit",
        Approve => "Approve",
        Reject => "Reject",
        Aggregate => "Aggregate",
        Amend => "Amend",
        _ => action
    };

    public static string BadgeClass(string action) => action switch
    {
        Create => "bg-success",
        Update => "bg-primary",
        Delete => "bg-danger",
        View => "bg-secondary",
        Export => "bg-info",
        Login or Logout => "bg-dark",
        Submit => "bg-warning",
        Approve => "bg-success",
        Reject => "bg-danger",
        Aggregate => "bg-info",
        Amend => "bg-warning",
        _ => "bg-secondary"
    };

    public static IEnumerable<string> All => new[] { Create, Update, Delete, View, Export, Login, Logout, Submit, Approve, Reject, Aggregate, Amend };
    public static IEnumerable<string> DataChanges => new[] { Create, Update, Delete };
    public static IEnumerable<string> WorkflowActions => new[] { Submit, Approve, Reject, Amend };
}

/// <summary>
/// Entity types that are audited.
/// </summary>
public static class AuditEntityType
{
    public const string User = "User";
    public const string OrganizationalUnit = "OrganizationalUnit";
    public const string Delegation = "Delegation";
    public const string ReportTemplate = "ReportTemplate";
    public const string ReportField = "ReportField";
    public const string ReportPeriod = "ReportPeriod";
    public const string Report = "Report";
    public const string ReportFieldValue = "ReportFieldValue";
    public const string SuggestedAction = "SuggestedAction";
    public const string ResourceRequest = "ResourceRequest";
    public const string SupportRequest = "SupportRequest";
    public const string Comment = "Comment";
    public const string ConfirmationTag = "ConfirmationTag";
    public const string Feedback = "Feedback";
    public const string Recommendation = "Recommendation";
    public const string Decision = "Decision";
    public const string AggregationRule = "AggregationRule";
    public const string AggregatedValue = "AggregatedValue";
    public const string ManagerAmendment = "ManagerAmendment";

    public static IEnumerable<string> All => new[]
    {
        User, OrganizationalUnit, Delegation, ReportTemplate, ReportField, ReportPeriod,
        Report, ReportFieldValue, SuggestedAction, ResourceRequest, SupportRequest,
        Comment, ConfirmationTag, Feedback, Recommendation, Decision,
        AggregationRule, AggregatedValue, ManagerAmendment
    };
}
