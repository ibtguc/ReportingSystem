using System.ComponentModel.DataAnnotations;

namespace ReportingSystem.Models;

public class ReportTemplate
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// If set, this template applies only to the specified report type.
    /// </summary>
    public ReportType? ReportType { get; set; }

    /// <summary>
    /// If set, this template applies to committees at this hierarchy level.
    /// </summary>
    public HierarchyLevel? HierarchyLevel { get; set; }

    /// <summary>
    /// If set, this template applies only to a specific committee.
    /// </summary>
    public int? CommitteeId { get; set; }

    /// <summary>
    /// Default body content template (pre-filled when user selects this template).
    /// Supports rich text / HTML.
    /// </summary>
    public string? BodyTemplate { get; set; }

    /// <summary>
    /// Whether the SuggestedAction section is included (shown) in this template.
    /// </summary>
    public bool IncludeSuggestedAction { get; set; } = true;

    /// <summary>
    /// Whether the NeededResources section is included.
    /// </summary>
    public bool IncludeNeededResources { get; set; } = true;

    /// <summary>
    /// Whether the NeededSupport section is included.
    /// </summary>
    public bool IncludeNeededSupport { get; set; } = true;

    /// <summary>
    /// Whether the SpecialRemarks section is included.
    /// </summary>
    public bool IncludeSpecialRemarks { get; set; } = true;

    /// <summary>
    /// Whether SuggestedAction is required when using this template.
    /// </summary>
    public bool RequireSuggestedAction { get; set; }

    /// <summary>
    /// Whether NeededResources is required when using this template.
    /// </summary>
    public bool RequireNeededResources { get; set; }

    /// <summary>
    /// Whether NeededSupport is required when using this template.
    /// </summary>
    public bool RequireNeededSupport { get; set; }

    /// <summary>
    /// Whether SpecialRemarks is required when using this template.
    /// </summary>
    public bool RequireSpecialRemarks { get; set; }

    /// <summary>
    /// One of the 5 default system templates (cannot be deleted by users).
    /// </summary>
    public bool IsDefault { get; set; }

    public bool IsActive { get; set; } = true;

    public int CreatedById { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public User CreatedBy { get; set; } = null!;
    public Committee? Committee { get; set; }
}
