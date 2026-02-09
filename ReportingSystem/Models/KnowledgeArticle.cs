using System.ComponentModel.DataAnnotations;

namespace ReportingSystem.Models;

public enum KnowledgeSourceType
{
    Report,
    Directive,
    MeetingDecision,
    Manual
}

public class KnowledgeArticle
{
    public int Id { get; set; }

    [Required]
    [StringLength(300)]
    public string Title { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Summary { get; set; }

    /// <summary>
    /// Full article content (HTML from rich text).
    /// </summary>
    [Required]
    public string Content { get; set; } = string.Empty;

    public int CategoryId { get; set; }

    /// <summary>
    /// Source type that generated this article.
    /// </summary>
    public KnowledgeSourceType SourceType { get; set; }

    /// <summary>
    /// ID of the source item (ReportId, DirectiveId, or MeetingDecisionId).
    /// </summary>
    public int? SourceItemId { get; set; }

    /// <summary>
    /// Committee that owns this knowledge.
    /// </summary>
    public int? CommitteeId { get; set; }

    /// <summary>
    /// Tags for search (comma-separated).
    /// </summary>
    [StringLength(500)]
    public string? Tags { get; set; }

    public int CreatedById { get; set; }

    public bool IsPublished { get; set; } = true;

    public int ViewCount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public KnowledgeCategory Category { get; set; } = null!;
    public Committee? Committee { get; set; }
    public User CreatedBy { get; set; } = null!;
}
