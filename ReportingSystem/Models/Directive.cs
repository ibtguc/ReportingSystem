using System.ComponentModel.DataAnnotations;

namespace ReportingSystem.Models;

public enum DirectiveType
{
    Instruction,
    Approval,
    CorrectiveAction,
    Feedback,
    InformationNotice
}

public enum DirectivePriority
{
    Normal,
    High,
    Urgent
}

public enum DirectiveStatus
{
    Issued,
    Delivered,
    Acknowledged,
    InProgress,
    Implemented,
    Verified,
    Closed
}

public class Directive
{
    public int Id { get; set; }

    [Required]
    [StringLength(300)]
    public string Title { get; set; } = string.Empty;

    public DirectiveType DirectiveType { get; set; } = DirectiveType.Instruction;

    public DirectivePriority Priority { get; set; } = DirectivePriority.Normal;

    public DirectiveStatus Status { get; set; } = DirectiveStatus.Issued;

    /// <summary>
    /// The user who issued this directive.
    /// </summary>
    public int IssuerId { get; set; }

    /// <summary>
    /// Target committee for this directive.
    /// </summary>
    public int TargetCommitteeId { get; set; }

    /// <summary>
    /// Optional specific target user within the committee.
    /// </summary>
    public int? TargetUserId { get; set; }

    /// <summary>
    /// Optional related report that prompted this directive.
    /// </summary>
    public int? RelatedReportId { get; set; }

    /// <summary>
    /// Parent directive for propagation chain (self-referential FK).
    /// When a committee head forwards a directive downward, a child directive is created.
    /// </summary>
    public int? ParentDirectiveId { get; set; }

    /// <summary>
    /// Main directive body content.
    /// </summary>
    public string BodyContent { get; set; } = string.Empty;

    /// <summary>
    /// Annotation added when forwarding (e.g., Chairman's Office annotates before sending to L0).
    /// </summary>
    [StringLength(2000)]
    public string? ForwardingAnnotation { get; set; }

    public DateTime? Deadline { get; set; }

    public bool IsConfidential { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public DateTime? AcknowledgedAt { get; set; }

    public DateTime? ImplementedAt { get; set; }

    // Navigation properties
    public User Issuer { get; set; } = null!;
    public Committee TargetCommittee { get; set; } = null!;
    public User? TargetUser { get; set; }
    public Report? RelatedReport { get; set; }
    public Directive? ParentDirective { get; set; }
    public ICollection<Directive> ChildDirectives { get; set; } = new List<Directive>();
    public ICollection<DirectiveStatusHistory> StatusHistory { get; set; } = new List<DirectiveStatusHistory>();
}
