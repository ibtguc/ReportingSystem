using System.ComponentModel.DataAnnotations;

namespace SchedulingSystem.Models;

/// <summary>
/// Represents a teacher absence (sick, personal, professional development, etc.)
/// </summary>
public class Absence
{
    public int Id { get; set; }

    [Required]
    public int TeacherId { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateTime Date { get; set; }

    /// <summary>
    /// Start time for partial day absence. Null = all day
    /// </summary>
    [DataType(DataType.Time)]
    public TimeSpan? StartTime { get; set; }

    /// <summary>
    /// End time for partial day absence. Null = all day
    /// </summary>
    [DataType(DataType.Time)]
    public TimeSpan? EndTime { get; set; }

    [Required]
    public AbsenceType Type { get; set; }

    [Required]
    public AbsenceStatus Status { get; set; } = AbsenceStatus.Reported;

    [StringLength(500)]
    public string? Notes { get; set; }

    [Required]
    public DateTime ReportedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User who reported the absence (admin/coordinator)
    /// </summary>
    public string? ReportedByUserId { get; set; }

    /// <summary>
    /// Total hours of absence (calculated for payroll)
    /// </summary>
    public decimal TotalHours { get; set; }

    // Navigation properties
    public Teacher Teacher { get; set; } = null!;
    public ApplicationUser? ReportedByUser { get; set; }
    public ICollection<Substitution> Substitutions { get; set; } = new List<Substitution>();
    public ICollection<BreakSupervisionSubstitution> SupervisionSubstitutions { get; set; } = new List<BreakSupervisionSubstitution>();
}

/// <summary>
/// Type of teacher absence
/// </summary>
public enum AbsenceType
{
    Sick,              // Illness/medical
    Personal,          // Personal day
    Professional,      // Training, conference, workshop
    Meeting,           // School meeting, department meeting
    Emergency,         // Family emergency, urgent matter
    Vacation,          // Planned vacation/leave
    AdministrativeDuty, // Teacher on-site but has admin duties requiring substitute coverage
    Other              // Other reasons
}

/// <summary>
/// Status of absence coverage
/// </summary>
public enum AbsenceStatus
{
    Reported,          // Just reported, not yet processed
    Confirmed,         // Confirmed by admin
    BeingCovered,      // Admin is finding substitutes
    Covered,           // All lessons have substitutes
    PartiallyCovered,  // Some lessons covered, some not
    NotCovered         // No substitutes available
}
