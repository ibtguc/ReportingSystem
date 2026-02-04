using System.ComponentModel.DataAnnotations;

namespace SchedulingSystem.Models;

/// <summary>
/// Junction entity for many-to-many relationship between ScheduledLesson and Room.
/// Allows a scheduled lesson to be held in multiple rooms simultaneously (co-teaching scenarios).
/// </summary>
public class ScheduledLessonRoom
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "Scheduled Lesson")]
    public int ScheduledLessonId { get; set; }

    [Required]
    [Display(Name = "Room")]
    public int RoomId { get; set; }

    /// <summary>
    /// Optional: Indicates which teacher is primarily using this room (for co-teaching).
    /// Links to either Lesson.TeacherId or Lesson.SecondTeacherId.
    /// </summary>
    [Display(Name = "Primary Teacher for Room")]
    public int? PrimaryTeacherIdForRoom { get; set; }

    /// <summary>
    /// Optional: Notes specific to this room assignment.
    /// e.g., "Group A - Advanced students", "Practical activities"
    /// </summary>
    [StringLength(500)]
    [Display(Name = "Room Assignment Notes")]
    public string? Notes { get; set; }

    /// <summary>
    /// Optional: Estimated number of students in this specific room.
    /// Useful for tracking capacity across multiple rooms.
    /// </summary>
    [Display(Name = "Student Count in Room")]
    public int? StudentCount { get; set; }

    // Navigation properties
    public ScheduledLesson? ScheduledLesson { get; set; }
    public Room? Room { get; set; }
    public Teacher? PrimaryTeacherForRoom { get; set; }

    /// <summary>
    /// Specific lesson assignments (teacher-subject-class combinations) in this room.
    /// If empty, fall back to showing all participants for this room.
    /// </summary>
    public ICollection<ScheduledLessonRoomAssignment> RoomAssignments { get; set; } = new List<ScheduledLessonRoomAssignment>();
}
