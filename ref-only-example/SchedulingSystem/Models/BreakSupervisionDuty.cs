using System.ComponentModel.DataAnnotations;

namespace SchedulingSystem.Models;

/// <summary>
/// Represents a teacher's break supervision duty assignment
/// Maps to GPU009.TXT from UNTIS export
///
/// GPU009.TXT columns:
/// 1. Corridor (Room) - supervision location
/// 2. Teacher - assigned teacher name
/// 3. Day Number - day of week (1=Monday, 5=Friday)
/// 4. Period Number - period when supervision occurs
/// 5. Points - point value for the duty
/// </summary>
public class BreakSupervisionDuty
{
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to Room - supervision locations are rooms in the system
    /// (e.g., "Hof1", "Hof2", "oben", "unten" are stored as rooms)
    /// Maps to GPU009 Column 1: Corridor
    /// </summary>
    [Required]
    [Display(Name = "Location")]
    public int RoomId { get; set; }

    /// <summary>
    /// Foreign key to teacher (nullable for unassigned slots)
    /// Maps to GPU009 Column 2: Teacher
    /// </summary>
    [Display(Name = "Teacher")]
    public int? TeacherId { get; set; }

    /// <summary>
    /// Day of week for the supervision duty
    /// Maps to GPU009 Column 3: Day Number (1=Monday to 5=Friday)
    /// </summary>
    [Required]
    [Display(Name = "Day of Week")]
    public DayOfWeek DayOfWeek { get; set; }

    /// <summary>
    /// Period number when supervision occurs (e.g., 3, 5)
    /// Maps to GPU009 Column 4: Period Number
    /// </summary>
    [Required]
    [Display(Name = "Period")]
    public int PeriodNumber { get; set; }

    /// <summary>
    /// Point value for the supervision duty
    /// Maps to GPU009 Column 5: Points
    /// </summary>
    [Required]
    [Display(Name = "Points")]
    public int Points { get; set; } = 30;

    [StringLength(500)]
    [Display(Name = "Notes")]
    public string? Notes { get; set; }

    [Display(Name = "Is Active")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Foreign key to timetable - supervision duties belong to specific timetables
    /// Similar to how ScheduledLessons belong to timetables
    /// </summary>
    [Display(Name = "Timetable")]
    public int? TimetableId { get; set; }

    // Navigation properties
    public Room? Room { get; set; }
    public Teacher? Teacher { get; set; }
    public Timetable? Timetable { get; set; }

    /// <summary>
    /// Gets a display string for the period
    /// </summary>
    public string PeriodDisplay => $"Period {PeriodNumber}";

    /// <summary>
    /// Gets a display string for the time slot (Day + Period)
    /// </summary>
    public string TimeSlotDisplay => $"{DayOfWeek} - {PeriodDisplay}";
}
