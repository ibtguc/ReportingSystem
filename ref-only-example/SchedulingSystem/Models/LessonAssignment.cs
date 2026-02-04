using System.ComponentModel.DataAnnotations;

namespace SchedulingSystem.Models;

/// <summary>
/// Represents a specific teacher-subject-class combination within a lesson.
/// Used when a lesson has multiple teachers, subjects, or classes and you need
/// to specify which teacher teaches which subject to which class.
///
/// This is optional - if no LessonAssignments exist for a lesson, the system
/// falls back to showing all combinations (current behavior).
/// </summary>
public class LessonAssignment
{
    public int Id { get; set; }

    /// <summary>
    /// The lesson this assignment belongs to
    /// </summary>
    [Required]
    public int LessonId { get; set; }

    /// <summary>
    /// The teacher assigned to this combination (optional)
    /// </summary>
    public int? TeacherId { get; set; }

    /// <summary>
    /// The subject assigned to this combination (optional)
    /// </summary>
    public int? SubjectId { get; set; }

    /// <summary>
    /// The class assigned to this combination (optional)
    /// </summary>
    public int? ClassId { get; set; }

    /// <summary>
    /// Optional notes for this assignment
    /// </summary>
    [StringLength(500)]
    public string? Notes { get; set; }

    /// <summary>
    /// Display order for sorting assignments
    /// </summary>
    public int Order { get; set; } = 0;

    // Navigation properties
    public Lesson? Lesson { get; set; }
    public Teacher? Teacher { get; set; }
    public Subject? Subject { get; set; }
    public Class? Class { get; set; }
    public ICollection<ScheduledLessonRoomAssignment> RoomAssignments { get; set; } = new List<ScheduledLessonRoomAssignment>();
}
