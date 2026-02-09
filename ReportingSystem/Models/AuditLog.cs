using System.ComponentModel.DataAnnotations;

namespace ReportingSystem.Models;

public class AuditLog
{
    public int Id { get; set; }

    [Required]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public int? UserId { get; set; }

    [StringLength(200)]
    public string? UserName { get; set; }

    [Required]
    public AuditActionType ActionType { get; set; }

    [Required]
    [StringLength(50)]
    public string ItemType { get; set; } = string.Empty;

    public int? ItemId { get; set; }

    [StringLength(500)]
    public string? ItemTitle { get; set; }

    [StringLength(2000)]
    public string? BeforeValue { get; set; }

    [StringLength(2000)]
    public string? AfterValue { get; set; }

    [StringLength(500)]
    public string? Details { get; set; }

    [StringLength(50)]
    public string? IpAddress { get; set; }

    [StringLength(100)]
    public string? SessionId { get; set; }

    public int? CommitteeId { get; set; }
}

public enum AuditActionType
{
    // Auth
    Login,
    Logout,

    // CRUD
    Create,
    Update,
    Delete,

    // Status transitions
    StatusChange,

    // Access
    AccessGranted,
    AccessDenied,
    AccessRevoked,

    // Confidentiality
    ConfidentialityMarked,
    ConfidentialityUnmarked,

    // Role/Membership
    RoleChanged,
    MembershipAdded,
    MembershipRemoved,

    // Search
    SearchPerformed,

    // Export
    Export,

    // Meeting-specific
    MeetingStarted,
    MinutesSubmitted,
    MinutesConfirmed,
    MinutesFinalized,

    // Directive-specific
    DirectiveForwarded,
    DirectiveAcknowledged,

    // Report-specific
    ReportApproved
}
