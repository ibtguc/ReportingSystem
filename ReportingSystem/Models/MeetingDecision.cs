using System.ComponentModel.DataAnnotations;

namespace ReportingSystem.Models;

public enum DecisionType
{
    Approval,
    Direction,
    Resolution,
    Deferral
}

public class MeetingDecision
{
    public int Id { get; set; }

    public int MeetingId { get; set; }

    /// <summary>
    /// Optional link to the agenda item this decision relates to.
    /// </summary>
    public int? AgendaItemId { get; set; }

    [Required]
    [StringLength(2000)]
    public string DecisionText { get; set; } = string.Empty;

    public DecisionType DecisionType { get; set; } = DecisionType.Resolution;

    public DateTime? Deadline { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Meeting Meeting { get; set; } = null!;
    public MeetingAgendaItem? AgendaItem { get; set; }
    public ICollection<ActionItem> ActionItems { get; set; } = new List<ActionItem>();
}
