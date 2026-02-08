using System.ComponentModel.DataAnnotations;

namespace ReportingSystem.Models;

public enum SystemRole
{
    SystemAdmin,
    Chairman,
    ChairmanOffice,
    CommitteeUser
}

/// <summary>
/// User account for system access
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

    public SystemRole SystemRole { get; set; } = SystemRole.CommitteeUser;

    [StringLength(100)]
    public string? Title { get; set; }

    [StringLength(20)]
    public string? Phone { get; set; }

    /// <summary>
    /// Rank within Chairman's Office (1=senior/Chief of Staff, 4=junior). Null if not in Chairman's Office.
    /// </summary>
    public int? ChairmanOfficeRank { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastLoginAt { get; set; }

    // Navigation properties
    public ICollection<MagicLink> MagicLinks { get; set; } = new List<MagicLink>();
    public ICollection<CommitteeMembership> CommitteeMemberships { get; set; } = new List<CommitteeMembership>();
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
