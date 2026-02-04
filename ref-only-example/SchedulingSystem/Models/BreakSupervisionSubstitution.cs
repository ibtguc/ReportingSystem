using System.ComponentModel.DataAnnotations;

namespace SchedulingSystem.Models;

/// <summary>
/// Represents a temporary substitution for a break supervision duty during a teacher absence.
/// Similar to Substitution model but for supervision duties instead of lessons.
/// </summary>
public class BreakSupervisionSubstitution
{
    public int Id { get; set; }

    /// <summary>
    /// Links to the teacher absence that triggered this substitution
    /// </summary>
    [Required]
    public int AbsenceId { get; set; }

    /// <summary>
    /// The break supervision duty being covered
    /// </summary>
    [Required]
    public int BreakSupervisionDutyId { get; set; }

    /// <summary>
    /// The substitute teacher assigned. Null if uncovered/cancelled.
    /// </summary>
    public int? SubstituteTeacherId { get; set; }

    /// <summary>
    /// Type of supervision substitution coverage
    /// </summary>
    [Required]
    public SupervisionSubstitutionType Type { get; set; } = SupervisionSubstitutionType.TeacherSubstitute;

    /// <summary>
    /// The specific date of the substitution (derived from Absence.Date)
    /// </summary>
    [Required]
    public DateTime Date { get; set; }

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

    // Navigation properties
    public Absence Absence { get; set; } = null!;
    public BreakSupervisionDuty BreakSupervisionDuty { get; set; } = null!;
    public Teacher? SubstituteTeacher { get; set; }
    public ApplicationUser? AssignedByUser { get; set; }
}

/// <summary>
/// Type of supervision substitution coverage
/// </summary>
public enum SupervisionSubstitutionType
{
    /// <summary>
    /// Another teacher covers the supervision duty
    /// </summary>
    TeacherSubstitute,

    /// <summary>
    /// Supervision cancelled (location unmanned)
    /// </summary>
    Cancelled,

    /// <summary>
    /// Another nearby supervisor will cover this area
    /// </summary>
    CombinedArea
}
