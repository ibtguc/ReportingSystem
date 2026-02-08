using System.ComponentModel.DataAnnotations;

namespace SchedulingSystem.Models;

/// <summary>
/// Represents a class/group of students
/// Maps to GPU003.TXT from UNTIS export
/// </summary>
public class Class
{
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    [Display(Name = "Class Code")]
    public string Name { get; set; } = string.Empty; // Short name from UNTIS

    [StringLength(200)]
    [Display(Name = "Full Name")]
    public string? FullName { get; set; }

    [StringLength(100)]
    [Display(Name = "Home Room")]
    public string? HomeRoom { get; set; }

    [Display(Name = "Min Periods Per Day")]
    public int? MinPeriodsPerDay { get; set; }

    [Display(Name = "Max Periods Per Day")]
    public int? MaxPeriodsPerDay { get; set; }

    [Display(Name = "Min Lunch Break")]
    public int? MinLunchBreak { get; set; }

    [Display(Name = "Max Lunch Break")]
    public int? MaxLunchBreak { get; set; }

    [Display(Name = "Max Consecutive Subjects")]
    public int? MaxConsecutiveSubjects { get; set; }

    [Display(Name = "Consecutive Main Subjects")]
    public int? ConsecutiveMainSubjects { get; set; }

    [Display(Name = "Class Level")]
    public int? ClassLevel { get; set; }

    [Display(Name = "Year Level")]
    public int YearLevel { get; set; }

    [Display(Name = "Male Students")]
    public int? MaleStudents { get; set; }

    [Display(Name = "Female Students")]
    public int? FemaleStudents { get; set; }

    [Display(Name = "Student Count")]
    public int StudentCount => (MaleStudents ?? 0) + (FemaleStudents ?? 0);

    // UNTIS Statistical fields
    [StringLength(50)]
    [Display(Name = "Statistic 1")]
    public string? Statistic1 { get; set; }

    [StringLength(50)]
    [Display(Name = "Statistic 2")]
    public string? Statistic2 { get; set; }

    // Date range for lessons
    [Display(Name = "Lesson Start Date")]
    [DataType(DataType.Date)]
    public DateTime? LessonStartDate { get; set; }

    [Display(Name = "Lesson End Date")]
    [DataType(DataType.Date)]
    public DateTime? LessonEndDate { get; set; }

    // Class Teacher reference
    [Display(Name = "Class Teacher")]
    public int? ClassTeacherId { get; set; }

    // Department reference
    [Display(Name = "Department")]
    public int? DepartmentId { get; set; }

    [StringLength(500)]
    [Display(Name = "Description/Notes")]
    public string? Description { get; set; }

    [StringLength(20)]
    [Display(Name = "Foreground Color")]
    public string? ForegroundColor { get; set; }

    [StringLength(20)]
    [Display(Name = "Background Color")]
    public string? BackgroundColor { get; set; }

    [StringLength(50)]
    [Display(Name = "Class Type")]
    public string? ClassType { get; set; }

    [Display(Name = "Parent Class")]
    public int? ParentClassId { get; set; }

    [Display(Name = "Is Active")]
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public Teacher? ClassTeacher { get; set; }
    public Department? Department { get; set; }
    public Class? ParentClass { get; set; }

    /// <summary>
    /// Sub-classes under this class (for hierarchical class structures).
    /// </summary>
    public ICollection<Class> SubClasses { get; set; } = new List<Class>();

    /// <summary>
    /// Lessons for this class (many-to-many via LessonClass).
    /// A class can have many lessons, and lessons can have multiple classes (combined classes).
    /// </summary>
    public ICollection<LessonClass> LessonClasses { get; set; } = new List<LessonClass>();

    /// <summary>
    /// Students enrolled in this class.
    /// </summary>
    public ICollection<Student> Students { get; set; } = new List<Student>();
}
