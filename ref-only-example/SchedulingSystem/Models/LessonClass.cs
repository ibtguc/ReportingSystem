using System.ComponentModel.DataAnnotations;

namespace SchedulingSystem.Models;

/// <summary>
/// Junction table for many-to-many relationship between Lesson and Class.
/// Allows a lesson to be taught to multiple classes simultaneously (e.g., combined classes).
/// </summary>
public class LessonClass
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "Lesson")]
    public int LessonId { get; set; }

    [Required]
    [Display(Name = "Class")]
    public int ClassId { get; set; }

    /// <summary>
    /// Indicates if this is the primary class for the lesson.
    /// Used for reporting and scheduling priorities.
    /// </summary>
    [Display(Name = "Is Primary Class")]
    public bool IsPrimary { get; set; } = false;

    /// <summary>
    /// Order of this class in the lesson (for display purposes).
    /// Lower numbers appear first.
    /// </summary>
    [Display(Name = "Display Order")]
    public int Order { get; set; } = 0;

    /// <summary>
    /// Number of students from this class attending the lesson.
    /// Optional - useful for capacity planning in combined classes.
    /// </summary>
    [Display(Name = "Student Count")]
    public int? StudentCount { get; set; }

    /// <summary>
    /// Optional notes about this class's participation in the lesson.
    /// E.g., "Group A only", "Advanced students", "Combined with 2B"
    /// </summary>
    [StringLength(500)]
    [Display(Name = "Notes")]
    public string? Notes { get; set; }

    // Navigation properties
    public Lesson? Lesson { get; set; }
    public Class? Class { get; set; }
}
