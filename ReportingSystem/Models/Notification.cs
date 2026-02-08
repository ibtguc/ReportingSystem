using System.ComponentModel.DataAnnotations;

namespace ReportingSystem.Models;

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
    /// Related entity ID (e.g., ReportId, RequestId)
    /// </summary>
    public int? RelatedEntityId { get; set; }
}

/// <summary>
/// Type of notification
/// </summary>
public enum NotificationType
{
    // Reports
    ReportSubmitted,         // Report has been submitted
    ReportStatusChanged,     // Report status changed
    ReportApproved,          // Report has been approved
    ReportRejected,          // Report has been rejected
    FeedbackReceived,        // New feedback from management

    // Directives
    DirectiveIssued,         // New directive issued
    DirectiveDelivered,      // Directive delivered to target
    DirectiveStatusChanged,  // Directive status changed

    // Meetings
    MeetingInvitation,       // Invited to a meeting
    MinutesSubmitted,        // Minutes submitted for confirmation
    ConfirmationRequested,   // Tagged for confirmation

    // Action Items
    ActionItemAssigned,      // Action item assigned to you
    ActionItemOverdue,       // Action item is overdue

    // Confidentiality
    ConfidentialityChanged,  // Confidentiality marking affects access

    // Legacy/General
    DecisionMade,            // Decision on a request
    RecommendationIssued,    // New recommendation received
    DeadlineApproaching,     // Submission deadline approaching
    General                  // General announcement
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
