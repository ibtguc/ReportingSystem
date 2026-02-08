using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReportingSystem.Models;

/// <summary>
/// Defines the structure of a report that users fill out.
/// Templates are versioned and can be assigned to org units, roles, or individuals.
/// </summary>
public class ReportTemplate
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    [Display(Name = "Template Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Required]
    [Display(Name = "Schedule")]
    [StringLength(50)]
    public string Schedule { get; set; } = ReportSchedule.Monthly;

    [Display(Name = "Version")]
    public int Version { get; set; } = 1;

    [StringLength(500)]
    [Display(Name = "Version Notes")]
    public string? VersionNotes { get; set; }

    [Display(Name = "Include Suggested Actions Section")]
    public bool IncludeSuggestedActions { get; set; } = true;

    [Display(Name = "Include Needed Resources Section")]
    public bool IncludeNeededResources { get; set; } = true;

    [Display(Name = "Include Needed Support Section")]
    public bool IncludeNeededSupport { get; set; } = true;

    [Display(Name = "Auto-Save Interval (seconds)")]
    [Range(10, 600)]
    public int AutoSaveIntervalSeconds { get; set; } = 60;

    [Display(Name = "Allow Pre-Population")]
    public bool AllowPrePopulation { get; set; } = true;

    [Display(Name = "Allow Bulk Import")]
    public bool AllowBulkImport { get; set; } = false;

    [Display(Name = "Max Attachment Size (MB)")]
    [Range(1, 100)]
    public int MaxAttachmentSizeMb { get; set; } = 10;

    [StringLength(500)]
    [Display(Name = "Allowed File Types")]
    public string? AllowedFileTypes { get; set; } = ".pdf,.doc,.docx,.xls,.xlsx,.csv,.png,.jpg,.jpeg";

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    [Display(Name = "Created By")]
    public int? CreatedById { get; set; }

    // Navigation properties
    public User? CreatedBy { get; set; }
    public ICollection<ReportField> Fields { get; set; } = new List<ReportField>();
    public ICollection<ReportTemplateAssignment> Assignments { get; set; } = new List<ReportTemplateAssignment>();
    public ICollection<ReportPeriod> Periods { get; set; } = new List<ReportPeriod>();
    public ICollection<Report> Reports { get; set; } = new List<Report>();

    /// <summary>
    /// Get ordered fields for display.
    /// </summary>
    [NotMapped]
    public IEnumerable<ReportField> OrderedFields =>
        Fields.OrderBy(f => f.SectionOrder).ThenBy(f => f.FieldOrder);
}

/// <summary>
/// Assigns a template to an org unit, role, or individual user.
/// </summary>
public class ReportTemplateAssignment
{
    public int Id { get; set; }

    [Required]
    public int ReportTemplateId { get; set; }

    [Required]
    [StringLength(50)]
    [Display(Name = "Assignment Type")]
    public string AssignmentType { get; set; } = TemplateAssignmentType.OrgUnit;

    /// <summary>
    /// The ID of the target (OrgUnit ID, or User ID depending on AssignmentType).
    /// For Role assignments, this is null and RoleValue is used instead.
    /// </summary>
    [Display(Name = "Target ID")]
    public int? TargetId { get; set; }

    /// <summary>
    /// For Role-based assignments, the role string value.
    /// </summary>
    [StringLength(50)]
    [Display(Name = "Role")]
    public string? RoleValue { get; set; }

    [Display(Name = "Include Sub-Units")]
    public bool IncludeSubUnits { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ReportTemplate ReportTemplate { get; set; } = null!;
}

/// <summary>
/// Report scheduling frequency options.
/// </summary>
public static class ReportSchedule
{
    public const string Daily = "Daily";
    public const string Weekly = "Weekly";
    public const string BiWeekly = "BiWeekly";
    public const string Monthly = "Monthly";
    public const string Quarterly = "Quarterly";
    public const string Annual = "Annual";
    public const string Custom = "Custom";

    public static readonly string[] All =
    [
        Daily, Weekly, BiWeekly, Monthly, Quarterly, Annual, Custom
    ];

    public static string DisplayName(string schedule) => schedule switch
    {
        Daily => "Daily",
        Weekly => "Weekly",
        BiWeekly => "Bi-Weekly",
        Monthly => "Monthly",
        Quarterly => "Quarterly",
        Annual => "Annual",
        Custom => "Custom",
        _ => schedule
    };
}

/// <summary>
/// Template assignment types.
/// </summary>
public static class TemplateAssignmentType
{
    public const string OrgUnit = "OrgUnit";
    public const string Role = "Role";
    public const string Individual = "Individual";

    public static readonly string[] All = [OrgUnit, Role, Individual];

    public static string DisplayName(string type) => type switch
    {
        OrgUnit => "Organizational Unit",
        Role => "Role",
        Individual => "Individual User",
        _ => type
    };
}
