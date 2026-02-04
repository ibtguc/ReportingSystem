using System.ComponentModel.DataAnnotations;

namespace SchedulingSystem.Models;

/// <summary>
/// Represents a room/classroom in the school
/// Maps to GPU005.TXT from UNTIS export
/// </summary>
public class Room
{
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    [Display(Name = "Room Code")]
    public string RoomNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    [Display(Name = "Room Name")]
    public string Name { get; set; } = string.Empty;

    // Alternative Room reference (self-referential)
    [Display(Name = "Alternative Room")]
    public int? AlternativeRoomId { get; set; }

    [StringLength(50)]
    [Display(Name = "Room Type")]
    public string? RoomType { get; set; }

    [Display(Name = "Room Weight")]
    public int? RoomWeight { get; set; }

    [Display(Name = "Capacity")]
    public int Capacity { get; set; }

    // Department reference
    [Display(Name = "Department")]
    public int? DepartmentId { get; set; }

    [StringLength(100)]
    [Display(Name = "Building/Corridor 1")]
    public string? Building { get; set; }

    [StringLength(100)]
    [Display(Name = "Floor/Corridor 2")]
    public string? Floor { get; set; }

    [StringLength(500)]
    [Display(Name = "Facilities/Equipment")]
    public string? Facilities { get; set; }

    [StringLength(500)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [StringLength(20)]
    [Display(Name = "Foreground Color")]
    public string? ForegroundColor { get; set; }

    [StringLength(20)]
    [Display(Name = "Background Color")]
    public string? BackgroundColor { get; set; }

    [Display(Name = "Is Active")]
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public Room? AlternativeRoom { get; set; }
    public Department? Department { get; set; }
    public ICollection<RoomAvailability> Availabilities { get; set; } = new List<RoomAvailability>();

    /// <summary>
    /// Legacy: Collection of scheduled lessons using this room (one-to-many).
    /// For backward compatibility with single-room assignments.
    /// </summary>
    public ICollection<ScheduledLesson> ScheduledLessons { get; set; } = new List<ScheduledLesson>();

    /// <summary>
    /// Modern: Collection of scheduled lesson room assignments (many-to-many).
    /// Use this for querying all lessons that use this room, including multi-room scenarios.
    /// </summary>
    public ICollection<ScheduledLessonRoom> ScheduledLessonRooms { get; set; } = new List<ScheduledLessonRoom>();
}
