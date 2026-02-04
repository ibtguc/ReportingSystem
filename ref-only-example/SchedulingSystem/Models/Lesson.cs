using System.ComponentModel.DataAnnotations;

namespace SchedulingSystem.Models;

/// <summary>
/// Represents a lesson definition (Subject + Class + Teacher combination)
/// This is the template, not a scheduled instance
/// </summary>
public class Lesson
{
    public int Id { get; set; }

    // NOTE: Subject, Class, and Teacher relationships are now many-to-many
    // See LessonSubject, LessonClass, and LessonTeacher junction tables

    [Display(Name = "Duration (Periods)")]
    public int Duration { get; set; } = 1;

    [Display(Name = "Frequency Per Week")]
    public int FrequencyPerWeek { get; set; } = 1;

    [Display(Name = "Class Periods Per Week")]
    public int? ClassPeriodsPerWeek { get; set; }

    [Display(Name = "Teacher Periods Per Week")]
    public int? TeacherPeriodsPerWeek { get; set; }

    [Display(Name = "Number of Students")]
    public int? NumberOfStudents { get; set; }

    [Display(Name = "Male Students")]
    public int? MaleStudents { get; set; }

    [Display(Name = "Female Students")]
    public int? FemaleStudents { get; set; }

    [Display(Name = "Week Value")]
    public decimal? WeekValue { get; set; }

    [Display(Name = "Year Value")]
    public decimal? YearValue { get; set; }

    [Display(Name = "From Date")]
    [DataType(DataType.Date)]
    public DateTime? FromDate { get; set; }

    [Display(Name = "To Date")]
    [DataType(DataType.Date)]
    public DateTime? ToDate { get; set; }

    [Display(Name = "Partition Number")]
    public int? PartitionNumber { get; set; }

    [StringLength(200)]
    [Display(Name = "Weekly Periods in Terms")]
    public string? WeeklyPeriodsInTerms { get; set; } // e.g., '2,4,0,2,3'

    [StringLength(100)]
    [Display(Name = "Student Group")]
    public string? StudentGroup { get; set; }

    [StringLength(100)]
    [Display(Name = "Home Room")]
    public string? HomeRoom { get; set; }

    [StringLength(50)]
    [Display(Name = "Required Room Type")]
    public string? RequiredRoomType { get; set; }

    [Display(Name = "Min Double Periods")]
    public int? MinDoublePeriods { get; set; }

    [Display(Name = "Max Double Periods")]
    public int? MaxDoublePeriods { get; set; }

    [Display(Name = "Block Size")]
    public int? BlockSize { get; set; }

    [Display(Name = "Priority")]
    public int? Priority { get; set; }

    [Display(Name = "Consecutive Subjects (Class)")]
    public int? ConsecutiveSubjectsClass { get; set; }

    [Display(Name = "Consecutive Subjects (Teacher)")]
    public int? ConsecutiveSubjectsTeacher { get; set; }

    [StringLength(100)]
    [Display(Name = "Codes")]
    public string? Codes { get; set; }

    [StringLength(500)]
    [Display(Name = "Special Requirements")]
    public string? SpecialRequirements { get; set; }

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

    // Navigation properties - Many-to-Many relationships
    /// <summary>
    /// Subjects taught in this lesson (many-to-many via LessonSubject).
    /// Most lessons have one subject, but integrated lessons can have multiple.
    /// </summary>
    public ICollection<LessonSubject> LessonSubjects { get; set; } = new List<LessonSubject>();

    /// <summary>
    /// Classes taking this lesson (many-to-many via LessonClass).
    /// Most lessons have one class, but combined classes are supported.
    /// </summary>
    public ICollection<LessonClass> LessonClasses { get; set; } = new List<LessonClass>();

    /// <summary>
    /// Teachers teaching this lesson (many-to-many via LessonTeacher).
    /// Supports single teacher or co-teaching scenarios with multiple teachers.
    /// </summary>
    public ICollection<LessonTeacher> LessonTeachers { get; set; } = new List<LessonTeacher>();

    /// <summary>
    /// Scheduled instances of this lesson in timetables.
    /// </summary>
    public ICollection<ScheduledLesson> ScheduledLessons { get; set; } = new List<ScheduledLesson>();

    /// <summary>
    /// Specific teacher-subject-class combinations within this lesson.
    /// Used when you need to specify which teacher teaches which subject to which class.
    /// If empty, fall back to showing all combinations.
    /// </summary>
    public ICollection<LessonAssignment> LessonAssignments { get; set; } = new List<LessonAssignment>();
}
