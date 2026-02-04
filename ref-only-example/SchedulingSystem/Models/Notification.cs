using System.ComponentModel.DataAnnotations;

namespace SchedulingSystem.Models;

/// <summary>
/// Represents an in-app notification for users
/// </summary>
public class Notification
{
    public int Id { get; set; }

    /// <summary>
    /// User who should receive this notification
    /// </summary>
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public NotificationType Type { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(1000)]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Optional URL to navigate to when notification is clicked
    /// </summary>
    [StringLength(500)]
    public string? ActionUrl { get; set; }

    /// <summary>
    /// Whether the user has read this notification
    /// </summary>
    public bool IsRead { get; set; } = false;

    /// <summary>
    /// When the notification was created
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the notification was read
    /// </summary>
    public DateTime? ReadAt { get; set; }

    /// <summary>
    /// Priority level for display
    /// </summary>
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

    /// <summary>
    /// Related entity ID (e.g., AbsenceId, SubstitutionId)
    /// </summary>
    public int? RelatedEntityId { get; set; }

    // Navigation properties
    public ApplicationUser User { get; set; } = null!;
}

/// <summary>
/// Type of notification
/// </summary>
public enum NotificationType
{
    AbsenceReported,        // Teacher absence reported
    SubstituteAssigned,     // You've been assigned as substitute
    SubstitutionApproaching, // Substitution starting soon (1 hour)
    AbsenceApproved,        // Your absence request approved
    AbsenceDenied,          // Your absence request denied
    CoverageComplete,       // All substitutions covered
    CoverageIncomplete,     // Some lessons still need coverage
    ScheduleChanged,        // Your schedule changed
    General                 // General announcement
}

/// <summary>
/// Priority level for notifications
/// </summary>
public enum NotificationPriority
{
    Low,        // Informational
    Normal,     // Standard notification
    High,       // Important - needs attention
    Urgent      // Critical - immediate action required
}
