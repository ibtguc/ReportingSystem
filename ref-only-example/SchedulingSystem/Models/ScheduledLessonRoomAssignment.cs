using System.ComponentModel.DataAnnotations;

namespace SchedulingSystem.Models;

/// <summary>
/// Links a specific LessonAssignment to a room in a scheduled lesson.
/// Specifies which teacher-subject-class combination is taught in which room.
///
/// This is optional - if no ScheduledLessonRoomAssignments exist for a
/// ScheduledLessonRoom, the system falls back to showing all participants
/// for that room (current behavior).
/// </summary>
public class ScheduledLessonRoomAssignment
{
    public int Id { get; set; }

    /// <summary>
    /// The scheduled lesson room this assignment belongs to
    /// </summary>
    [Required]
    public int ScheduledLessonRoomId { get; set; }

    /// <summary>
    /// The lesson assignment that specifies which teacher/subject/class is in this room
    /// </summary>
    [Required]
    public int LessonAssignmentId { get; set; }

    // Navigation properties
    public ScheduledLessonRoom? ScheduledLessonRoom { get; set; }
    public LessonAssignment? LessonAssignment { get; set; }
}
