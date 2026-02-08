using System.ComponentModel.DataAnnotations;

namespace SchedulingSystem.Models;

/// <summary>
/// Represents a scheduled instance of a lesson (actual timetable entry)
/// </summary>
public class ScheduledLesson
{
    public int Id { get; set; }

    // Foreign keys
    [Required]
    [Display(Name = "Lesson")]
    public int LessonId { get; set; }

    [Required]
    [Display(Name = "Day of Week")]
    public DayOfWeek DayOfWeek { get; set; }

    [Required]
    [Display(Name = "Period")]
    public int PeriodId { get; set; }

    /// <summary>
    /// Legacy: Single room assignment (for backward compatibility).
    /// For lessons with multiple rooms, this can be the primary room or null.
    /// Use ScheduledLessonRooms collection for multi-room scenarios.
    /// </summary>
    [Display(Name = "Room (Legacy)")]
    public int? RoomId { get; set; }

    [Display(Name = "Week Number")]
    public int? WeekNumber { get; set; }

    [Display(Name = "Timetable")]
    public int? TimetableId { get; set; }

    /// <summary>
    /// Indicates whether this scheduled lesson is locked/fixed in place.
    /// Locked lessons won't be changed during regeneration.
    /// </summary>
    [Display(Name = "Is Locked")]
    public bool IsLocked { get; set; } = false;

    // Navigation properties
    public Lesson? Lesson { get; set; }
    public Period? Period { get; set; }

    /// <summary>
    /// Legacy: Single room navigation (for backward compatibility).
    /// </summary>
    public Room? Room { get; set; }

    public Timetable? Timetable { get; set; }

    /// <summary>
    /// Modern: Collection of rooms for this scheduled lesson.
    /// Use this for co-teaching scenarios where lesson spans multiple rooms.
    /// </summary>
    public ICollection<ScheduledLessonRoom> ScheduledLessonRooms { get; set; } = new List<ScheduledLessonRoom>();
}
