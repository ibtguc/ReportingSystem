using System.ComponentModel.DataAnnotations;

namespace ReportingSystem.Models;

public enum ActionItemStatus
{
    Assigned,
    InProgress,
    Completed,
    Verified
}

public class ActionItem
{
    public int Id { get; set; }

    public int MeetingId { get; set; }

    /// <summary>
    /// Optional link to the decision that generated this action item.
    /// </summary>
    public int? MeetingDecisionId { get; set; }

    [Required]
    [StringLength(300)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    public int AssignedToId { get; set; }

    public int AssignedById { get; set; }

    public ActionItemStatus Status { get; set; } = ActionItemStatus.Assigned;

    public DateTime? Deadline { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime? VerifiedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public Meeting Meeting { get; set; } = null!;
    public MeetingDecision? MeetingDecision { get; set; }
    public User AssignedTo { get; set; } = null!;
    public User AssignedBy { get; set; } = null!;
}
