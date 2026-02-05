using System.ComponentModel.DataAnnotations;

namespace ReportingSystem.Models;

/// <summary>
/// Represents a comment or discussion on a report.
/// Supports threading (replies) and @mentions.
/// </summary>
public class Comment
{
    public int Id { get; set; }

    [Required]
    public int ReportId { get; set; }

    /// <summary>
    /// Parent comment ID for threading (null for top-level comments).
    /// </summary>
    public int? ParentCommentId { get; set; }

    [Required]
    public int AuthorId { get; set; }

    [Required]
    [StringLength(4000)]
    [Display(Name = "Content")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Optional reference to a specific section of the report.
    /// </summary>
    [StringLength(100)]
    [Display(Name = "Section Reference")]
    public string? SectionReference { get; set; }

    /// <summary>
    /// Optional reference to a specific field ID.
    /// </summary>
    public int? ReportFieldId { get; set; }

    /// <summary>
    /// JSON array of mentioned user IDs extracted from @mentions in content.
    /// </summary>
    [StringLength(500)]
    public string? MentionedUserIdsJson { get; set; }

    [Required]
    [StringLength(30)]
    public string Status { get; set; } = CommentStatus.Active;

    public bool IsEdited { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    // Navigation properties
    public Report Report { get; set; } = null!;
    public Comment? ParentComment { get; set; }
    public ICollection<Comment> Replies { get; set; } = new List<Comment>();
    public User Author { get; set; } = null!;
    public ReportField? ReportField { get; set; }

    /// <summary>
    /// Check if comment is a reply to another comment.
    /// </summary>
    public bool IsReply => ParentCommentId.HasValue;

    /// <summary>
    /// Check if comment is deleted (soft delete).
    /// </summary>
    public bool IsDeleted => Status == CommentStatus.Deleted;
}

/// <summary>
/// Comment status values.
/// </summary>
public static class CommentStatus
{
    public const string Active = "Active";
    public const string Edited = "Edited";
    public const string Deleted = "Deleted";
    public const string Hidden = "Hidden";

    public static readonly string[] All = [Active, Edited, Deleted, Hidden];

    public static string DisplayName(string status) => status switch
    {
        Active => "Active",
        Edited => "Edited",
        Deleted => "Deleted",
        Hidden => "Hidden",
        _ => status
    };

    public static string BadgeClass(string status) => status switch
    {
        Active => "bg-success",
        Edited => "bg-info",
        Deleted => "bg-secondary",
        Hidden => "bg-warning",
        _ => "bg-secondary"
    };
}
