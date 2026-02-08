using System.ComponentModel.DataAnnotations;

namespace SchedulingSystem.Models;

/// <summary>
/// Represents a subject/course taught in the school
/// Maps to GPU006.TXT from UNTIS export
/// </summary>
public class Subject
{
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    [Display(Name = "Subject Code")]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    [Display(Name = "Subject Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(100)]
    [Display(Name = "Category/Subject Group")]
    public string? Category { get; set; }

    // Preferred Room reference
    [Display(Name = "Preferred Room")]
    public int? PreferredRoomId { get; set; }

    [Display(Name = "Min Periods Per Week")]
    public int? MinPeriodsPerWeek { get; set; }

    [Display(Name = "Max Periods Per Week")]
    public int? MaxPeriodsPerWeek { get; set; }

    [Display(Name = "Min Periods Per Day")]
    public int? MinPeriodsPerDay { get; set; }

    [Display(Name = "Max Periods Per Day")]
    public int? MaxPeriodsPerDay { get; set; }

    [Display(Name = "Consecutive Periods (Class)")]
    public int? ConsecutivePeriodsClass { get; set; }

    [Display(Name = "Consecutive Periods (Teacher)")]
    public int? ConsecutivePeriodsTeacher { get; set; }

    [Display(Name = "Default Duration (Periods)")]
    public int DefaultDuration { get; set; } = 1;

    [StringLength(50)]
    [Display(Name = "Required Room Type")]
    public string? RequiredRoomType { get; set; }

    [Display(Name = "Factor")]
    public decimal? Factor { get; set; } // Weighting factor for scheduling

    // Department reference
    [Display(Name = "Department")]
    public int? DepartmentId { get; set; }

    [StringLength(500)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [StringLength(20)]
    [Display(Name = "Foreground Color")]
    public string? ForegroundColor { get; set; }

    [StringLength(20)]
    [Display(Name = "Background Color")]
    public string? BackgroundColor { get; set; }

    [StringLength(20)]
    [Display(Name = "Color (Hex)")]
    public string? Color { get; set; }

    [Display(Name = "Is Active")]
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public Room? PreferredRoom { get; set; }
    public Department? Department { get; set; }

    /// <summary>
    /// Lessons that teach this subject (many-to-many via LessonSubject).
    /// A subject can be taught in many lessons.
    /// </summary>
    public ICollection<LessonSubject> LessonSubjects { get; set; } = new List<LessonSubject>();

    /// <summary>
    /// Teachers qualified to teach this subject (many-to-many via TeacherSubject).
    /// </summary>
    public ICollection<TeacherSubject> TeacherSubjects { get; set; } = new List<TeacherSubject>();
}
