using System.ComponentModel.DataAnnotations;

namespace ReportingSystem.Models;

public enum MeetingType
{
    Regular,
    Emergency,
    Annual,
    SpecialSession
}

public enum MeetingStatus
{
    Scheduled,
    InProgress,
    MinutesEntry,
    MinutesReview,
    Finalized,
    Cancelled
}

public enum RecurrencePattern
{
    None,
    Weekly,
    Biweekly,
    Monthly
}

public class Meeting
{
    public int Id { get; set; }

    [Required]
    [StringLength(300)]
    public string Title { get; set; } = string.Empty;

    public MeetingType MeetingType { get; set; } = MeetingType.Regular;

    public MeetingStatus Status { get; set; } = MeetingStatus.Scheduled;

    /// <summary>
    /// The committee hosting this meeting.
    /// </summary>
    public int CommitteeId { get; set; }

    /// <summary>
    /// The designated moderator for this meeting.
    /// </summary>
    public int ModeratorId { get; set; }

    [StringLength(2000)]
    public string? Description { get; set; }

    [StringLength(500)]
    public string? Location { get; set; }

    public DateTime ScheduledAt { get; set; }

    /// <summary>
    /// Duration in minutes.
    /// </summary>
    public int DurationMinutes { get; set; } = 60;

    public RecurrencePattern RecurrencePattern { get; set; } = RecurrencePattern.None;

    /// <summary>
    /// Meeting minutes content (rich text, entered by moderator).
    /// </summary>
    public string? MinutesContent { get; set; }

    public DateTime? MinutesSubmittedAt { get; set; }

    public DateTime? MinutesFinalizedAt { get; set; }

    public bool IsConfidential { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public Committee Committee { get; set; } = null!;
    public User Moderator { get; set; } = null!;
    public ICollection<MeetingAgendaItem> AgendaItems { get; set; } = new List<MeetingAgendaItem>();
    public ICollection<MeetingAttendee> Attendees { get; set; } = new List<MeetingAttendee>();
    public ICollection<MeetingDecision> Decisions { get; set; } = new List<MeetingDecision>();
    public ICollection<ActionItem> ActionItems { get; set; } = new List<ActionItem>();
}
