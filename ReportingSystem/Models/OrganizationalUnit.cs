using System.ComponentModel.DataAnnotations;

namespace ReportingSystem.Models;

/// <summary>
/// Represents a node in the organizational hierarchy (Campus, Department, Sector, Team, etc.)
/// Supports unlimited depth via self-referential parent-child relationship.
/// </summary>
public class OrganizationalUnit
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    [Display(Name = "Unit Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(50)]
    [Display(Name = "Unit Code")]
    public string? Code { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    [Display(Name = "Level")]
    public OrgUnitLevel Level { get; set; } = OrgUnitLevel.Department;

    [Display(Name = "Parent Unit")]
    public int? ParentId { get; set; }

    [Display(Name = "Sort Order")]
    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    [Display(Name = "Parent Unit")]
    public OrganizationalUnit? Parent { get; set; }

    public ICollection<OrganizationalUnit> Children { get; set; } = new List<OrganizationalUnit>();

    public ICollection<User> Users { get; set; } = new List<User>();

    /// <summary>
    /// Returns a display string showing the hierarchy path (e.g., "Campus > Department > Team")
    /// Built from the unit's own name; full path requires loading ancestors.
    /// </summary>
    public string DisplayName => Level == OrgUnitLevel.Root
        ? Name
        : $"{Name} ({Level})";
}

/// <summary>
/// Defines the level of an organizational unit in the hierarchy.
/// Ordered from top (Root) to bottom (Team).
/// </summary>
public enum OrgUnitLevel
{
    [Display(Name = "Root / Organization")]
    Root = 0,

    [Display(Name = "Campus")]
    Campus = 1,

    [Display(Name = "Faculty / Division")]
    Faculty = 2,

    [Display(Name = "Department")]
    Department = 3,

    [Display(Name = "Sector / Section")]
    Sector = 4,

    [Display(Name = "Team / Unit")]
    Team = 5
}
