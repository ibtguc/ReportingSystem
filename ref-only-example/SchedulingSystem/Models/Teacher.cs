using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchedulingSystem.Models;

/// <summary>
/// Represents a teacher in the school
/// Maps to GPU004.TXT from UNTIS export
/// </summary>
public class Teacher
{
    public int Id { get; set; }

    // Name fields from UNTIS
    // UNTIS Field 1 "Name" = First Name
    // UNTIS Field 2 "Full Name" = Last Name
    [Required]
    [StringLength(100)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [StringLength(100)]
    [Display(Name = "Last Name")]
    public string? LastName { get; set; }

    [StringLength(50)]
    [Display(Name = "Title")]
    public string? Title { get; set; } // Mr., Mrs., Dr., etc.

    [Display(Name = "Date of Birth")]
    [DataType(DataType.Date)]
    public DateTime? DateOfBirth { get; set; }

    [StringLength(50)]
    [Display(Name = "Personnel Number")]
    public string? PersonnelNumber { get; set; }

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

    [Display(Name = "Max Consecutive Periods")]
    public int? MaxConsecutivePeriods { get; set; }

    [Display(Name = "Weekly Quota")]
    public decimal? WeeklyQuota { get; set; }

    [Display(Name = "Weekly Value")]
    public decimal? WeeklyValue { get; set; }

    [Display(Name = "Yearly Quota")]
    public decimal? YearlyQuota { get; set; }

    [Display(Name = "Min Non-Teaching Periods")]
    public int? MinNonTeachingPeriods { get; set; }

    [Display(Name = "Max Non-Teaching Periods")]
    public int? MaxNonTeachingPeriods { get; set; }

    // UNTIS Statistical fields
    [StringLength(50)]
    [Display(Name = "Statistic 1")]
    public string? Statistic1 { get; set; }

    [StringLength(50)]
    [Display(Name = "Statistic 2")]
    public string? Statistic2 { get; set; }

    [StringLength(50)]
    [Display(Name = "Status")]
    public string? Status { get; set; } // Employment status

    // Department reference
    [Display(Name = "Department")]
    public int? DepartmentId { get; set; }

    [StringLength(50)]
    [Display(Name = "Value Factor")]
    public string? ValueFactor { get; set; }

    [StringLength(20)]
    [Display(Name = "Gender")]
    public string? Gender { get; set; } // 1 = female, 2 = male from UNTIS

    [EmailAddress]
    [StringLength(200)]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [Phone]
    [StringLength(20)]
    [Display(Name = "Phone Number")]
    public string? PhoneNumber { get; set; }

    [Phone]
    [StringLength(20)]
    [Display(Name = "Mobile Number")]
    public string? MobileNumber { get; set; }

    [StringLength(500)]
    [Display(Name = "Description/Notes")]
    public string? Description { get; set; }

    [StringLength(20)]
    [Display(Name = "Foreground Color")]
    public string? ForegroundColor { get; set; }

    [StringLength(20)]
    [Display(Name = "Background Color")]
    public string? BackgroundColor { get; set; }

    [Display(Name = "Is Active")]
    public bool IsActive { get; set; } = true;

    // ========== Substitution Availability & Preferences ==========

    /// <summary>
    /// Teacher is available for substitution assignments
    /// </summary>
    [Display(Name = "Available for Substitutions")]
    public bool AvailableForSubstitution { get; set; } = true;

    /// <summary>
    /// Maximum substitution assignments per week
    /// </summary>
    [Range(0, 20)]
    [Display(Name = "Max Substitutions Per Week")]
    public int? MaxSubstitutionsPerWeek { get; set; } = 5;

    /// <summary>
    /// Optional: Hourly rate for payroll tracking
    /// Can be used for part-time or external teachers
    /// </summary>
    [Display(Name = "Substitution Hourly Rate")]
    public decimal? SubstitutionHourlyRate { get; set; }

    /// <summary>
    /// Substitution preferences and notes
    /// Examples: "Prefer morning periods", "Available Mon-Wed only", "Math subjects only"
    /// </summary>
    [StringLength(1000)]
    [Display(Name = "Substitution Preferences")]
    public string? SubstitutionPreferences { get; set; }

    /// <summary>
    /// Remark about subject qualifications for substitution
    /// Optional - used to note unofficial qualifications
    /// Example: "Can teach basic Math and Science", "Comfortable with Grade 9-10 English"
    /// </summary>
    [StringLength(500)]
    [Display(Name = "Substitution Qualification Notes")]
    public string? SubstitutionQualificationNotes { get; set; }

    /// <summary>
    /// Preferred notification method for substitution assignments
    /// </summary>
    [Display(Name = "Preferred Notification Method")]
    public NotificationPreference PreferredNotification { get; set; } = NotificationPreference.Email;

    // ========== Computed Properties ==========

    /// <summary>
    /// Full name for display purposes
    /// </summary>
    [NotMapped]
    [Display(Name = "Full Name")]
    public string FullName => !string.IsNullOrEmpty(LastName)
        ? $"{FirstName} {LastName}".Trim()
        : FirstName;

    /// <summary>
    /// Display name - uses full name if available, otherwise first name only
    /// </summary>
    [NotMapped]
    [Display(Name = "Display Name")]
    public string DisplayName => FullName;

    /// <summary>
    /// Short name for compact displays (uses first name only)
    /// </summary>
    [NotMapped]
    [Display(Name = "Short Name")]
    public string ShortName => FirstName;

    /// <summary>
    /// Name property for backward compatibility with existing code
    /// Returns FirstName as the primary identifier
    /// </summary>
    [NotMapped]
    [Display(Name = "Name")]
    public string Name => FirstName;

    // Backward compatibility - MaxHoursPerWeek mapped to WeeklyQuota
    [NotMapped]
    [Display(Name = "Max Hours Per Week")]
    public int MaxHoursPerWeek
    {
        get => (int)(WeeklyQuota ?? 40);
        set => WeeklyQuota = value;
    }

    // Navigation properties
    public Department? Department { get; set; }

    /// <summary>
    /// Lessons taught by this teacher (many-to-many via LessonTeacher).
    /// A teacher can teach many lessons, and lessons can have multiple teachers (co-teaching).
    /// </summary>
    public ICollection<LessonTeacher> LessonTeachers { get; set; } = new List<LessonTeacher>();

    /// <summary>
    /// Availability preferences/constraints for this teacher.
    /// </summary>
    public ICollection<TeacherAvailability> Availabilities { get; set; } = new List<TeacherAvailability>();

    /// <summary>
    /// Subjects this teacher is qualified to teach (many-to-many via TeacherSubject).
    /// </summary>
    public ICollection<TeacherSubject> TeacherSubjects { get; set; } = new List<TeacherSubject>();

    /// <summary>
    /// Break supervision duties assigned to this teacher.
    /// Used for scheduling and substitution planning.
    /// </summary>
    public ICollection<BreakSupervisionDuty> BreakSupervisionDuties { get; set; } = new List<BreakSupervisionDuty>();
}

/// <summary>
/// Preferred notification method for substitution assignments
/// </summary>
public enum NotificationPreference
{
    Email,          // Email only
    SMS,            // SMS/Text message
    Both,           // Email and SMS
    Phone           // Phone call (manual)
}
