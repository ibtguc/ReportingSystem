using System.ComponentModel.DataAnnotations;

namespace ReportingSystem.Models;

public enum ReportType
{
    Detailed,
    Summary,
    ExecutiveSummary
}

public enum ReportStatus
{
    Draft,
    Submitted,
    UnderReview,
    FeedbackRequested,
    Revised,
    Summarized,
    Approved,
    Archived
}

public class Report
{
    public int Id { get; set; }

    [Required]
    [StringLength(300)]
    public string Title { get; set; } = string.Empty;

    public ReportType ReportType { get; set; } = ReportType.Detailed;

    public ReportStatus Status { get; set; } = ReportStatus.Draft;

    public int AuthorId { get; set; }

    public int CommitteeId { get; set; }

    /// <summary>
    /// Main report body content (rich text / HTML).
    /// </summary>
    public string BodyContent { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? SuggestedAction { get; set; }

    [StringLength(1000)]
    public string? NeededResources { get; set; }

    [StringLength(1000)]
    public string? NeededSupport { get; set; }

    [StringLength(1000)]
    public string? SpecialRemarks { get; set; }

    public bool IsConfidential { get; set; } = false;

    public DateTime? SubmittedAt { get; set; }

    /// <summary>
    /// Version number — increments on each revision after feedback.
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// If this is a revision, links to the original report.
    /// </summary>
    public int? OriginalReportId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public User Author { get; set; } = null!;
    public Committee Committee { get; set; } = null!;
    public Report? OriginalReport { get; set; }
    public ICollection<Report> Revisions { get; set; } = new List<Report>();
    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
    public ICollection<ReportStatusHistory> StatusHistory { get; set; } = new List<ReportStatusHistory>();

    /// <summary>
    /// When this report IS a summary — links to the source reports it summarizes.
    /// </summary>
    public ICollection<ReportSourceLink> SourceLinks { get; set; } = new List<ReportSourceLink>();

    /// <summary>
    /// When this report IS a source — links to summaries that reference it.
    /// </summary>
    public ICollection<ReportSourceLink> SummaryLinks { get; set; } = new List<ReportSourceLink>();
}
