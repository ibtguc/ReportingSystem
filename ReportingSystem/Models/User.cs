using System.ComponentModel.DataAnnotations;

namespace ReportingSystem.Models;

/// <summary>
/// User account for system access
/// Currently supports administrators only
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
    public string Role { get; set; } = "Administrator"; // Administrator, Manager, Employee (future)

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastLoginAt { get; set; }

    // Navigation properties
    public ICollection<MagicLink> MagicLinks { get; set; } = new List<MagicLink>();
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
