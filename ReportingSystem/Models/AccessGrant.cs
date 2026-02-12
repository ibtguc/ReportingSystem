using System.ComponentModel.DataAnnotations;

namespace ReportingSystem.Models;

/// <summary>
/// Explicit access grant for confidential items.
/// FR-4.5.3.4: Allows sharing confidential items with specific users who
/// wouldn't otherwise have access through the hierarchy rules.
/// </summary>
public class AccessGrant
{
    public int Id { get; set; }

    public ConfidentialItemType ItemType { get; set; }

    /// <summary>
    /// The ID of the item being shared (Report.Id, Directive.Id, or Meeting.Id).
    /// </summary>
    public int ItemId { get; set; }

    /// <summary>
    /// The user receiving access to the confidential item.
    /// </summary>
    public int GrantedToUserId { get; set; }

    /// <summary>
    /// The user who granted access (typically the item owner or SystemAdmin).
    /// </summary>
    public int GrantedById { get; set; }

    [StringLength(500)]
    public string? Reason { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;

    public DateTime? RevokedAt { get; set; }

    public int? RevokedById { get; set; }

    // Navigation properties
    public User GrantedTo { get; set; } = null!;
    public User GrantedBy { get; set; } = null!;
    public User? RevokedBy { get; set; }
}
