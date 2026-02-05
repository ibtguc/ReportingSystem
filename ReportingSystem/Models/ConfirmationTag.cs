using System.ComponentModel.DataAnnotations;

namespace ReportingSystem.Models;

/// <summary>
/// Represents a request for another user to confirm/verify a report or section.
/// The originator tags a user to verify specific content before final submission.
/// </summary>
public class ConfirmationTag
{
    public int Id { get; set; }

    [Required]
    public int ReportId { get; set; }

    /// <summary>
    /// User who created the tag (requesting confirmation).
    /// </summary>
    [Required]
    public int RequestedById { get; set; }

    /// <summary>
    /// User being asked to confirm.
    /// </summary>
    [Required]
    public int TaggedUserId { get; set; }

    /// <summary>
    /// Optional reference to a specific section name.
    /// </summary>
    [StringLength(100)]
    [Display(Name = "Section")]
    public string? SectionReference { get; set; }

    /// <summary>
    /// Optional reference to a specific field.
    /// </summary>
    public int? ReportFieldId { get; set; }

    [StringLength(500)]
    [Display(Name = "Message")]
    public string? Message { get; set; }

    [Required]
    [StringLength(30)]
    public string Status { get; set; } = ConfirmationStatus.Pending;

    [StringLength(1000)]
    [Display(Name = "Response")]
    public string? Response { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? RespondedAt { get; set; }

    public DateTime? ReminderSentAt { get; set; }

    // Navigation properties
    public Report Report { get; set; } = null!;
    public User RequestedBy { get; set; } = null!;
    public User TaggedUser { get; set; } = null!;
    public ReportField? ReportField { get; set; }

    /// <summary>
    /// Check if confirmation is still pending.
    /// </summary>
    public bool IsPending => Status == ConfirmationStatus.Pending;

    /// <summary>
    /// Check if confirmation was positive.
    /// </summary>
    public bool IsConfirmed => Status == ConfirmationStatus.Confirmed;

    /// <summary>
    /// Days since request was created (for reminder logic).
    /// </summary>
    public int DaysSinceRequested => (DateTime.UtcNow - CreatedAt).Days;
}

/// <summary>
/// Confirmation tag status values.
/// </summary>
public static class ConfirmationStatus
{
    public const string Pending = "Pending";
    public const string Confirmed = "Confirmed";
    public const string RevisionRequested = "RevisionRequested";
    public const string Declined = "Declined";
    public const string Expired = "Expired";
    public const string Cancelled = "Cancelled";

    public static readonly string[] All = [Pending, Confirmed, RevisionRequested, Declined, Expired, Cancelled];

    public static string DisplayName(string status) => status switch
    {
        Pending => "Pending",
        Confirmed => "Confirmed",
        RevisionRequested => "Revision Requested",
        Declined => "Declined",
        Expired => "Expired",
        Cancelled => "Cancelled",
        _ => status
    };

    public static string BadgeClass(string status) => status switch
    {
        Pending => "bg-warning text-dark",
        Confirmed => "bg-success",
        RevisionRequested => "bg-info",
        Declined => "bg-danger",
        Expired => "bg-secondary",
        Cancelled => "bg-secondary",
        _ => "bg-secondary"
    };
}
