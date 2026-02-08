using System.ComponentModel.DataAnnotations;

namespace SchedulingSystem.Models;

/// <summary>
/// Junction table for many-to-many relationship between Lesson and Subject.
/// Allows a lesson to cover multiple subjects (e.g., integrated/cross-disciplinary lessons).
/// </summary>
public class LessonSubject
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "Lesson")]
    public int LessonId { get; set; }

    [Required]
    [Display(Name = "Subject")]
    public int SubjectId { get; set; }

    /// <summary>
    /// Indicates if this is the primary subject for the lesson.
    /// Used for reporting and display purposes.
    /// </summary>
    [Display(Name = "Is Primary Subject")]
    public bool IsPrimary { get; set; } = false;

    /// <summary>
    /// Order of this subject in the lesson (for display purposes).
    /// Lower numbers appear first.
    /// </summary>
    [Display(Name = "Display Order")]
    public int Order { get; set; } = 0;

    /// <summary>
    /// Optional notes about this subject's role in the lesson.
    /// E.g., "Main focus", "Supporting topic", "Integration component"
    /// </summary>
    [StringLength(500)]
    [Display(Name = "Notes")]
    public string? Notes { get; set; }

    // Navigation properties
    public Lesson? Lesson { get; set; }
    public Subject? Subject { get; set; }
}
