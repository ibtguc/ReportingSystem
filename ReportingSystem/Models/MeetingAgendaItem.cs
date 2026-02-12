using System.ComponentModel.DataAnnotations;

namespace ReportingSystem.Models;

public class MeetingAgendaItem
{
    public int Id { get; set; }

    public int MeetingId { get; set; }

    public int OrderIndex { get; set; }

    [Required]
    [StringLength(300)]
    public string TopicTitle { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    /// <summary>
    /// Allocated time for this agenda item in minutes.
    /// </summary>
    public int AllocatedMinutes { get; set; } = 15;

    /// <summary>
    /// Optional presenter for this item.
    /// </summary>
    public int? PresenterId { get; set; }

    /// <summary>
    /// Optional linked report for reference during discussion.
    /// </summary>
    public int? LinkedReportId { get; set; }

    /// <summary>
    /// Discussion notes captured during the meeting (part of minutes).
    /// </summary>
    public string? DiscussionNotes { get; set; }

    // Navigation properties
    public Meeting Meeting { get; set; } = null!;
    public User? Presenter { get; set; }
    public Report? LinkedReport { get; set; }
}
