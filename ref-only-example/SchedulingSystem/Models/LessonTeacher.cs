using System.ComponentModel.DataAnnotations;

namespace SchedulingSystem.Models;

/// <summary>
/// Junction table for many-to-many relationship between Lesson and Teacher.
/// Allows a lesson to have multiple teachers (co-teaching scenarios).
/// </summary>
public class LessonTeacher
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "Lesson")]
    public int LessonId { get; set; }

    [Required]
    [Display(Name = "Teacher")]
    public int TeacherId { get; set; }

    /// <summary>
    /// Indicates if this is the lead/primary teacher for the lesson.
    /// The lead teacher is typically responsible for planning and grading.
    /// </summary>
    [Display(Name = "Is Lead Teacher")]
    public bool IsLead { get; set; } = false;

    /// <summary>
    /// Order/priority of this teacher in the lesson (for display purposes).
    /// Lower numbers appear first. Lead teacher typically has order 0.
    /// </summary>
    [Display(Name = "Display Order")]
    public int Order { get; set; } = 0;

    /// <summary>
    /// Specific room assignment for this teacher (for multi-room co-teaching).
    /// Links to ScheduledLessonRoom.RoomId when lesson is scheduled.
    /// Null if not assigned to a specific room.
    /// </summary>
    [Display(Name = "Room Assignment")]
    public int? RoomAssignment { get; set; }

    /// <summary>
    /// Percentage of responsibility/workload for this teacher (optional).
    /// E.g., 50% for equal co-teaching, 25% for support teacher.
    /// Should sum to 100% across all teachers in a lesson.
    /// </summary>
    [Display(Name = "Workload Percentage")]
    [Range(0, 100)]
    public int? WorkloadPercentage { get; set; }

    /// <summary>
    /// Role description for this teacher in the lesson.
    /// E.g., "Lead instructor", "Support teacher", "Special education specialist"
    /// </summary>
    [StringLength(200)]
    [Display(Name = "Role")]
    public string? Role { get; set; }

    /// <summary>
    /// Optional notes about this teacher's involvement in the lesson.
    /// E.g., "Handles practical activities", "Supports group B", "Guest lecturer"
    /// </summary>
    [StringLength(500)]
    [Display(Name = "Notes")]
    public string? Notes { get; set; }

    // Navigation properties
    public Lesson? Lesson { get; set; }
    public Teacher? Teacher { get; set; }
    public Room? AssignedRoom { get; set; }
}
