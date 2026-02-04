using System.ComponentModel.DataAnnotations;

namespace ReportingSystem.Models;

/// <summary>
/// User account for system access.
/// Links to an OrganizationalUnit and has a system role.
/// </summary>
public class User
{
    public int Id { get; set; }

    [Required]
    [EmailAddress]
    [StringLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Role { get; set; } = "Administrator";

    [Display(Name = "Organizational Unit")]
    public int? OrganizationalUnitId { get; set; }

    [StringLength(100)]
    [Display(Name = "Job Title")]
    public string? JobTitle { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastLoginAt { get; set; }

    // Navigation properties
    [Display(Name = "Organizational Unit")]
    public OrganizationalUnit? OrganizationalUnit { get; set; }

    public ICollection<MagicLink> MagicLinks { get; set; } = new List<MagicLink>();
    public ICollection<Report> SubmittedReports { get; set; } = new List<Report>();
    public ICollection<Report> ReviewedReports { get; set; } = new List<Report>();
}

/// <summary>
/// Magic link token for passwordless authentication
/// </summary>
public class MagicLink
{
    public int Id { get; set; }

    public int UserId { get; set; }

    [Required]
    [StringLength(100)]
    public string Token { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAt { get; set; }

    public bool IsUsed { get; set; } = false;

    public DateTime? UsedAt { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    // Navigation property
    public User User { get; set; } = null!;
}

/// <summary>
/// Available system roles per the SRS.
/// Kept as string constants so they can be used in [Authorize] policies and claims.
/// </summary>
public static class SystemRoles
{
    public const string Administrator = "Administrator";
    public const string ReportOriginator = "ReportOriginator";
    public const string ReportReviewer = "ReportReviewer";
    public const string TeamManager = "TeamManager";
    public const string DepartmentHead = "DepartmentHead";
    public const string Executive = "Executive";
    public const string Auditor = "Auditor";

    public static readonly string[] All =
    [
        Administrator,
        ReportOriginator,
        ReportReviewer,
        TeamManager,
        DepartmentHead,
        Executive,
        Auditor
    ];

    /// <summary>
    /// Display-friendly name for a role.
    /// </summary>
    public static string DisplayName(string role) => role switch
    {
        Administrator => "Administrator",
        ReportOriginator => "Report Originator",
        ReportReviewer => "Report Reviewer",
        TeamManager => "Team Manager",
        DepartmentHead => "Department Head",
        Executive => "Executive",
        Auditor => "Auditor",
        _ => role
    };
}
