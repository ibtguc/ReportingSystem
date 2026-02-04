using System.ComponentModel.DataAnnotations;

namespace SchedulingSystem.Models;

/// <summary>
/// Represents a substitution assignment for a specific lesson during a teacher absence
/// </summary>
public class Substitution
{
    public int Id { get; set; }

    [Required]
    public int AbsenceId { get; set; }

    [Required]
    public int ScheduledLessonId { get; set; }

    /// <summary>
    /// The substitute teacher assigned. Null if self-study, cancelled, etc.
    /// </summary>
    public int? SubstituteTeacherId { get; set; }

    [Required]
    public SubstitutionType Type { get; set; } = SubstitutionType.TeacherSubstitute;

    [StringLength(500)]
    public string? Notes { get; set; }

    /// <summary>
    /// When the substitution was assigned
    /// </summary>
    [Required]
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Admin/coordinator who made the assignment
    /// </summary>
    public string? AssignedByUserId { get; set; }

    /// <summary>
    /// Whether email notification was sent to substitute
    /// </summary>
    public bool EmailSent { get; set; } = false;

    /// <summary>
    /// When email notification was sent
    /// </summary>
    public DateTime? EmailSentAt { get; set; }

    /// <summary>
    /// Whether the substitute has accepted (future feature)
    /// </summary>
    public bool? IsAccepted { get; set; }

    /// <summary>
    /// When substitute accepted/declined (future feature)
    /// </summary>
    public DateTime? RespondedAt { get; set; }

    /// <summary>
    /// Hours worked for payroll (typically = lesson duration)
    /// </summary>
    public decimal HoursWorked { get; set; }

    /// <summary>
    /// Pay rate at time of substitution (for external substitutes)
    /// </summary>
    public decimal? PayRate { get; set; }

    /// <summary>
    /// Total pay for this substitution (HoursWorked * PayRate)
    /// </summary>
    public decimal? TotalPay { get; set; }

    // Navigation properties
    public Absence Absence { get; set; } = null!;
    public ScheduledLesson ScheduledLesson { get; set; } = null!;
    public Teacher? SubstituteTeacher { get; set; }
    public ApplicationUser? AssignedByUser { get; set; }
}

/// <summary>
/// Type of substitution coverage
/// </summary>
public enum SubstitutionType
{
    TeacherSubstitute,  // Another teacher covers the lesson
    ClassMerger,        // Class combined with another class
    SelfStudy,          // Supervised self-study/study hall
    Cancelled,          // Lesson cancelled (last resort)
    RoomChange,         // Same substitute, different room
    Rescheduled         // Lesson moved to different time
}
