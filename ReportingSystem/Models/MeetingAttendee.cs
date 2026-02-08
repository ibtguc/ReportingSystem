using System.ComponentModel.DataAnnotations;

namespace ReportingSystem.Models;

public enum RsvpStatus
{
    Pending,
    Accepted,
    Declined,
    Tentative
}

public enum ConfirmationStatus
{
    Pending,
    Confirmed,
    RevisionRequested,
    Abstained
}

public class MeetingAttendee
{
    public int Id { get; set; }

    public int MeetingId { get; set; }

    public int UserId { get; set; }

    public RsvpStatus RsvpStatus { get; set; } = RsvpStatus.Pending;

    [StringLength(500)]
    public string? RsvpComment { get; set; }

    public DateTime? RsvpAt { get; set; }

    public ConfirmationStatus ConfirmationStatus { get; set; } = ConfirmationStatus.Pending;

    [StringLength(1000)]
    public string? ConfirmationComment { get; set; }

    public DateTime? ConfirmedAt { get; set; }

    // Navigation properties
    public Meeting Meeting { get; set; } = null!;
    public User User { get; set; } = null!;
}
