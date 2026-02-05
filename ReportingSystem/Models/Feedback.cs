using System.ComponentModel.DataAnnotations;

namespace ReportingSystem.Models;

/// <summary>
/// Represents feedback from management on a report.
/// Used for recognition, concerns, observations, and questions flowing downward.
/// </summary>
public class Feedback
{
    public int Id { get; set; }

    [Required]
    public int ReportId { get; set; }

    /// <summary>
    /// User providing the feedback (typically a manager/reviewer).
    /// </summary>
    [Required]
    public int AuthorId { get; set; }

    [Required]
    [StringLength(200)]
    [Display(Name = "Subject")]
    public string Subject { get; set; } = string.Empty;

    [Required]
    [StringLength(4000)]
    [Display(Name = "Content")]
    public string Content { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    [Display(Name = "Category")]
    public string Category { get; set; } = FeedbackCategory.General;

    [Required]
    [StringLength(30)]
    [Display(Name = "Visibility")]
    public string Visibility { get; set; } = FeedbackVisibility.Private;

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

    /// <summary>
    /// Parent feedback for threading support.
    /// </summary>
    public int? ParentFeedbackId { get; set; }

    /// <summary>
    /// Whether the report originator has acknowledged this feedback.
    /// </summary>
    public bool IsAcknowledged { get; set; } = false;

    public DateTime? AcknowledgedAt { get; set; }

    /// <summary>
    /// Optional acknowledgment response from the report originator.
    /// </summary>
    [StringLength(1000)]
    [Display(Name = "Acknowledgment Response")]
    public string? AcknowledgmentResponse { get; set; }

    [Required]
    [StringLength(30)]
    public string Status { get; set; } = FeedbackStatus.Active;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public Report Report { get; set; } = null!;
    public User Author { get; set; } = null!;
    public ReportField? ReportField { get; set; }
    public Feedback? ParentFeedback { get; set; }
    public ICollection<Feedback> Replies { get; set; } = new List<Feedback>();

    /// <summary>
    /// Check if this is a reply to another feedback.
    /// </summary>
    public bool IsReply => ParentFeedbackId.HasValue;

    /// <summary>
    /// Check if feedback requires acknowledgment.
    /// </summary>
    public bool RequiresAcknowledgment => Category == FeedbackCategory.Concern ||
                                          Category == FeedbackCategory.Question;

    /// <summary>
    /// Check if feedback is pending acknowledgment.
    /// </summary>
    public bool IsPendingAcknowledgment => RequiresAcknowledgment && !IsAcknowledged;
}

/// <summary>
/// Categories for feedback.
/// </summary>
public static class FeedbackCategory
{
    public const string PositiveRecognition = "PositiveRecognition";
    public const string Concern = "Concern";
    public const string Observation = "Observation";
    public const string Question = "Question";
    public const string General = "General";

    public static readonly string[] All = [PositiveRecognition, Concern, Observation, Question, General];

    public static string DisplayName(string category) => category switch
    {
        PositiveRecognition => "Positive Recognition",
        Concern => "Concern",
        Observation => "Observation",
        Question => "Question",
        General => "General",
        _ => category
    };

    public static string BadgeClass(string category) => category switch
    {
        PositiveRecognition => "bg-success",
        Concern => "bg-danger",
        Observation => "bg-info",
        Question => "bg-warning text-dark",
        General => "bg-secondary",
        _ => "bg-secondary"
    };

    public static string Icon(string category) => category switch
    {
        PositiveRecognition => "bi-star-fill",
        Concern => "bi-exclamation-triangle-fill",
        Observation => "bi-eye-fill",
        Question => "bi-question-circle-fill",
        General => "bi-chat-fill",
        _ => "bi-chat"
    };
}

/// <summary>
/// Visibility levels for feedback.
/// </summary>
public static class FeedbackVisibility
{
    public const string Private = "Private";
    public const string TeamWide = "TeamWide";
    public const string DepartmentWide = "DepartmentWide";
    public const string OrganizationWide = "OrganizationWide";

    public static readonly string[] All = [Private, TeamWide, DepartmentWide, OrganizationWide];

    public static string DisplayName(string visibility) => visibility switch
    {
        Private => "Private (Report Author Only)",
        TeamWide => "Team-wide",
        DepartmentWide => "Department-wide",
        OrganizationWide => "Organization-wide",
        _ => visibility
    };

    public static string BadgeClass(string visibility) => visibility switch
    {
        Private => "bg-secondary",
        TeamWide => "bg-info",
        DepartmentWide => "bg-primary",
        OrganizationWide => "bg-success",
        _ => "bg-secondary"
    };
}

/// <summary>
/// Status values for feedback.
/// </summary>
public static class FeedbackStatus
{
    public const string Active = "Active";
    public const string Resolved = "Resolved";
    public const string Archived = "Archived";

    public static readonly string[] All = [Active, Resolved, Archived];

    public static string DisplayName(string status) => status;

    public static string BadgeClass(string status) => status switch
    {
        Active => "bg-primary",
        Resolved => "bg-success",
        Archived => "bg-secondary",
        _ => "bg-secondary"
    };
}
